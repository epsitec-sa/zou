// Copyright © 2013-2020, EPSITEC SA, CH-1400 Yverdon-les-Bains, Switzerland
// Author: Roger VUISTINER, Maintainer: Roger VUISTINER

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

using Bcx.IO;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Zou.Tasks
{
    public class Zip : Task
    {
        // <PropertyGroup>
        //     <ZipFolder>Source/Items</ZipFolder>
        // </PropertyGroup>

        public bool                     Overwrite { get; set; } = true;
        public bool                     Update    { get; set; }
        [Required] public string        FileName  { get; set; }
        [Required] public ITaskItem[]   Files     { get; set; }

        public override bool            Execute()
        {
            this.CreateZipFile(this.FileName, this.Files.Select(f => f.ItemSpec));
            return !this.Log.HasLoggedErrors;
        }

        private void                    CreateZipFile(string zipFileName, IEnumerable<string> fileNames)
        {
            try
            {
                var zipPath     = Path.GetFullPath(zipFileName);
                var zipBasePath = Path.GetDirectoryName(zipPath);

                if (this.Overwrite && File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }

                var archiveMode = this.Update ? ZipArchiveMode.Update : ZipArchiveMode.Create;

                using var zipFile = ZipFile.Open(zipPath, archiveMode);
                foreach (var fileName in fileNames)
                {
                    var fullPath = Path.GetFullPath(fileName);
                    var relPath  = PathEx.GetRelativePath(zipBasePath, fullPath);
                    zipFile.CreateEntryFromFile(fullPath, relPath, CompressionLevel.Optimal);
                }
            }
            catch(Exception e)
            {
                this.Log.LogErrorFromException(e);
            }
        }
    }
}
