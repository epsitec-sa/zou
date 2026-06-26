// Copyright © 2013-2021, EPSITEC SA, CH-1400 Yverdon-les-Bains, Switzerland
// Author: Roger VUISTINER, Maintainer: Roger VUISTINER

using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Zou.Tasks
{
    // Cross-process build lock based on an exclusively-opened lock file
    // (FileShare.None). Unlike named semaphores -- which are Windows-only and
    // throw PlatformNotSupportedException on Unix -- a lock file provides the
    // same mutual exclusion on every platform MSBuild runs on, with a single
    // code path. The acquired FileStream is kept open by the build engine and
    // released by the matching Unlock task.
    public class Lock : Task
    {
        public bool                 Global  { get; set; }
        public int                  Timeout { get; set; }
        [Required] public string    Name    { get; set; }

        public override bool        Execute()
        {
            try
            {
                string lockPath = Lock.GetLockFilePath(this.Name, this.Global);

                this.Log.LogMessageFromText($"Lock -> waiting lock '{lockPath}'.", MessageImportance.Normal);
                var stream = Lock.AcquireLock(lockPath, this.TimeoutInternal);

                // Register the stream with the build engine:
                // - to keep the lock held (stream open) until the end of the build,
                // - to make it available for the matching Unlock task.
                this.RegisterLock(lockPath, stream);
                this.Log.LogMessageFromText($"Lock -> entered lock '{lockPath}'.", MessageImportance.Normal);
            }
            catch (Exception e)
            {
                this.Log.LogErrorFromException(e);
            }
            return !this.Log.HasLoggedErrors;
        }

        private int                 TimeoutInternal => this.Timeout == 0 ? System.Threading.Timeout.Infinite : this.Timeout;
        private void                RegisterLock(string key, FileStream stream)
        {
            this.BuildEngine4.RegisterTaskObject(key, stream, RegisteredTaskObjectLifetime.AppDomain, false);
        }

        private static FileStream   AcquireLock(string path, int timeoutMs)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            var watch = Stopwatch.StartNew();
            while (true)
            {
                try
                {
                    // FileShare.None denies any concurrent open: a second process
                    // (or build node) gets an IOException and retries until release.
                    return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                }
                catch (IOException)
                {
                    if (timeoutMs != System.Threading.Timeout.Infinite && watch.ElapsedMilliseconds >= timeoutMs)
                    {
                        throw new TimeoutException($"Lock -> timeout waiting for lock file '{path}'.");
                    }
                    Thread.Sleep(Lock.PollIntervalMs);
                }
            }
        }

        internal static string      GetLockFilePath(string name, bool global)
        {
            // A readable (but bounded) prefix derived from the lock name, plus a
            // stable hash so distinct names never collide and the file-name
            // component stays within filesystem limits even for long paths.
            var safe = Regex.Replace(name, @"\W+", "_", RegexOptions.CultureInvariant).ToLowerInvariant();
            if (safe.Length > 64)
            {
                safe = safe.Substring(0, 64);
            }

            // The Global flag maps to a distinct lock identity (kept for API
            // parity with the former named-semaphore "Global\" namespace). Note
            // the lock file lives under the per-user temp directory, which is
            // sufficient for serializing parallel build nodes of a single user;
            // it is not shared across user sessions. Global is currently unused
            // by the build targets.
            var hash = Lock.GetStableHash((global ? "G|" : "L|") + name);
            var dir  = Path.Combine(Path.GetTempPath(), "zou-locks");
            return Path.Combine(dir, $"{safe}.{hash}.lock");
        }

        private static string       GetStableHash(string value)
        {
            // SHA1 (not String.GetHashCode, which is per-process randomized and
            // would yield different file names across processes) keeps the lock
            // identity consistent for every process contending on it.
            using (var sha1 = SHA1.Create())
            {
                var bytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(value));
                var sb    = new StringBuilder(16);
                for (var i = 0; i < 8; i++)
                {
                    sb.Append(bytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        private const int           PollIntervalMs = 50;
    }
}
