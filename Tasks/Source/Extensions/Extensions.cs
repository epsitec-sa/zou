// Copyright © 2013-2020, EPSITEC SA, CH-1400 Yverdon-les-Bains, Switzerland
// Author: Roger VUISTINER, Maintainer: Roger VUISTINER

using Bcx.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Zou.Tasks
{
    public static class DirectoryInfoExtensions
    {
        public static IEnumerable<DirectoryInfo> EnumerateDirectoriesSafe(this DirectoryInfo source)
        {
            return DirectoryEx
                .EnumerateDirectoriesSafe(source.FullName)
                .Select(x => new DirectoryInfo(x));
        }
        /// <summary>
        /// Create a subdirectory with next index available.
        /// </summary>
        /// <param name="source">The directory in which to create the subdirectory</param>
        /// <param name="index">The computed index</param>
        /// <param name="prefix">An optional prefix before the index</param>
        /// <param name="suffix">An optional suffix after the index</param>
        /// <param name="escaped">Indicates if the prefix and suffix input values are already escaped using Regex.Escape</param>
        /// <returns>The DirectoryInfo of the created subdirectory</returns>
        /// <remarks>
        /// The prefix and suffix input values are used in a regular expression to match existing directories.
        /// Set escaped value to true if these values are already escaped with Regex.Escape method.
        /// If escaped is false, the underlying implementation will escape them.
        /// </remarks>
        public static DirectoryInfo   CreateIndexedSubdirectory(this DirectoryInfo source, out int index, string prefix = null, string suffix = null, bool escaped = false)
        {
            source.CreateIndexedSubdirectory(out index, out var subdir, prefix, suffix, escaped);
            return subdir;
        }
        public static DirectoryInfo   CreateIndexedSubdirectory(this DirectoryInfo source, out DirectoryInfo subdirectory, string prefix = null, string suffix = null, bool escaped = false)
        {
            return source.CreateIndexedSubdirectory(out var index, out subdirectory, prefix, suffix, escaped);
        }
        public static DirectoryInfo   CreateIndexedSubdirectory(this DirectoryInfo source, out int index, out DirectoryInfo subdirectory, string prefix = null, string suffix = null, bool escaped = false)
        {
            subdirectory = source.GetIndexedSubdirectory(out index, prefix, suffix, escaped).EnsureExists();
            return source.Clone();
        }
        public static DirectoryInfo   EnsureExists(this DirectoryInfo source)
        {
            return source.CreateSafe();
        }
        public static DirectoryInfo   CreateSafe(this DirectoryInfo source)
        {
            return Helpers.Retry(() => Helpers.Create(source));
        }
        private static DirectoryInfo  GetIndexedSubdirectory(this DirectoryInfo source, out int index, string prefix, string suffix, bool escaped)
        {
            return new DirectoryInfo(source.GetIndexedSubdirectoryPath(out index, prefix, suffix, escaped));
        }
        private static string         GetIndexedSubdirectoryPath(this DirectoryInfo source, out int index, string prefix, string suffix, bool escaped)
        {
            var existingNames = source.EnumerateDirectoriesSafe().Select(dir => dir.Name);
            return source.GetIndexedSubpath(out index, existingNames, prefix, suffix, escaped);
        }
        private static string         GetIndexedSubpath(this DirectoryInfo source, out int index, IEnumerable<string> existingNames, string prefix, string suffix, bool escaped)
        {
            prefix = prefix ?? string.Empty;
            suffix = suffix ?? string.Empty;
            source.EnsureExists();

            var extractor = Helpers.NumberExtractorRegex(prefix, suffix, escaped);
            var indexes = Helpers.ExtractInt(existingNames, extractor);
            index = indexes.Any() ? indexes.Max() + 1 : 1;
            var name = prefix + index.ToString() + suffix;
            return source.GetSubpath(name);
        }
        public static string          GetSubpath(this DirectoryInfo source, string name)
        {
            return Path.Combine(source.FullName, name);
        }
        private static DirectoryInfo  Clone(this DirectoryInfo source)
        {
            // used for immutability (sort of)
            return new DirectoryInfo(source.FullName);
        }

        private static class Helpers
        {
            public static Regex             NumberExtractorRegex(string prefix, string suffix, bool escaped)
            {
                if (!escaped)
                {
                    prefix = Regex.Escape(prefix);
                    suffix = Regex.Escape(suffix);
                }
                var sb = new StringBuilder("^");
                sb.Append(prefix);
                sb.Append(@"(\d+)");
                sb.Append(suffix);
                sb.Append('$');
                return new Regex(sb.ToString(), RegexOptions.IgnoreCase);
            }
            public static int?              ExtractInt(string value, Regex extractor)
            {
                var match = extractor.Match(value);
                return match.Success ? int.Parse(match.Groups[1].Value) : default(int?);
            }
            public static IEnumerable<int>  ExtractInt(IEnumerable<string> values, Regex extractor)
            {
                return values.Select(v => Helpers.ExtractInt(v, extractor)).Where(i => i.HasValue).Select(i => i.Value);
            }
            public static DirectoryInfo     Create(DirectoryInfo source)
            {
                if (!source.Exists)
                {
                    var clone = source.Clone();
                    clone.Create();
                    return clone;
                }
                return source;
            }
            public static T                 Retry<T>(Func<T> func)
            {
                return func.RetryWithBackoffStrategy(10, TimeSpan.FromMilliseconds(10), retryPredicate: e => e is IOException || e is UnauthorizedAccessException);
            }

        }
    }

    public static class FunctionalExtensions
    {
        public static T RetryWithBackoffStrategy<T>(this Func<T> self, int retryCount, TimeSpan delay, Func<Exception, Exception> errorSelector = null, Func<Exception, bool> retryPredicate = null)
        {
            return self.RetryWithBackoffStrategy(retryCount, _ => delay, errorSelector, retryPredicate);
        }
        public static T RetryWithBackoffStrategy<T>(this Func<T> self, int retryCount, Func<int, TimeSpan> backoffStrategy = null, Func<Exception, Exception> errorSelector = null, Func<Exception, bool> retryPredicate = null)
        {
            backoffStrategy = backoffStrategy ?? (_ => TimeSpan.FromMilliseconds(10));
            errorSelector = errorSelector ?? (_ => _);
            retryPredicate = retryPredicate ?? (_ => true);

            for (int attempt = 1; ; ++attempt)
            {
                try
                {
                    return self();
                }
                catch (Exception e) when (attempt <= retryCount && errorSelector(e).ShouldRetry(attempt, retryPredicate, backoffStrategy))
                {
                }
            }
        }

        private static bool ShouldRetry(this Exception self, int attempt, Func<Exception, bool> retryPredicate, Func<int, TimeSpan> backoffStrategy)
        {
            var retry = retryPredicate(self);
            if (retry)
            {
                Thread.Sleep(Convert.ToInt32(backoffStrategy(attempt).TotalMilliseconds));
            }
            return retry;
        }
    }
}
