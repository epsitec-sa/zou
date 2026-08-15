// Copyright © 2013-2021, EPSITEC SA, CH-1400 Yverdon-les-Bains, Switzerland
// Author: Roger VUISTINER, Maintainer: Roger VUISTINER

using System;
using System.IO;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Zou.Tasks
{
    // Releases the cross-process lock acquired by the matching Lock task by
    // disposing the FileStream kept alive in the build engine registry. See
    // Lock for the rationale behind the lock-file approach.
    public class Unlock : Task
    {
        public bool                 Global { get; set; }
        [Required] public string    Name   { get; set; }

        public override bool        Execute()
        {
            try
            {
                var lockPath = Lock.GetLockFilePath(this.Name, this.Global);
                var stream   = this.UnregisterLock(lockPath);
                if (stream == null)
                {
                    this.Log.LogError($"Unlock -> lock '{lockPath}' not registered.");
                }
                else
                {
                    // Disposing the stream closes the handle and releases the
                    // FileShare.None lock; the lock file itself is left on disk
                    // for reuse (harmless, and avoids delete/recreate races).
                    stream.Dispose();
                    this.Log.LogMessageFromText($"Unlock -> released lock '{lockPath}'.", MessageImportance.Normal);
                }
            }
            catch (Exception e)
            {
                this.Log.LogErrorFromException(e);
            }
            return !this.Log.HasLoggedErrors;
        }

        private FileStream          UnregisterLock(string key)
        {
            return this.BuildEngine4.UnregisterTaskObject(key, RegisteredTaskObjectLifetime.AppDomain) as FileStream;
        }
    }
}
