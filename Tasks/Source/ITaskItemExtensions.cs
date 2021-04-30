// Copyright © 2013-2021, EPSITEC SA, CH-1400 Yverdon-les-Bains, Switzerland
// Author: Roger VUISTINER, Maintainer: Roger VUISTINER

using System.Collections.Generic;
using System.Linq;

using Microsoft.Build.Framework;

namespace Zou.Tasks
{
    public static class ITaskItemExtensions
    {
        public static IEnumerable<string>   GetMetadata(this ITaskItem self, IEnumerable<string> keys) => keys.Select(k => self.GetMetadata(k));
        public static string                JoinMetadata(this ITaskItem self, string keys) => string.Join(";", keys.Split(';').Select(k => self.GetMetadata(k)));
        public static IEnumerable<string>   CustomMetadataNames(this ITaskItem self) => self.MetadataNames.Cast<string>().Except(StandardMetadataNames);

        private static IEnumerable<string>  StandardMetadataNames
        {
            get
            {
                yield return "FullPath";
                yield return "RootDir";
                yield return "Filename";
                yield return "Extension";
                yield return "RelativeDir";
                yield return "Directory";
                yield return "RecursiveDir";
                yield return "Identity";
                yield return "ModifiedTime";
                yield return "CreatedTime";
                yield return "AccessedTime";
                yield return "DefiningProjectFullPath";
                yield return "DefiningProjectDirectory";
                yield return "DefiningProjectName";
                yield return "DefiningProjectExtension";
            }
        }
    }
}
