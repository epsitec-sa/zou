// Copyright © 2013-2020, EPSITEC SA, CH-1400 Yverdon-les-Bains, Switzerland
// Author: Roger VUISTINER, Maintainer: Roger VUISTINER

using Bcx.IO;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Zou.Tasks
{
    public class Zip : Task
    {
        public bool                     Overwrite { get; set; } = true;
        public bool                     Update    { get; set; }
        [Required] public string        FileName  { get; set; }
        [Required] public ITaskItem[]   Files     { get; set; }

        public override bool            Execute()
        {
            //Debugger.Launch();
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
                    var relPath = PathEx.GetRelativePath(zipBasePath, Path.GetFullPath(fileName));
                    zipFile.CreateEntryFromFile(relPath, relPath, CompressionLevel.Optimal);
                }
            }
            catch(Exception e)
            {
                this.Log.LogErrorFromException(e);
            }
        }
    }
}
