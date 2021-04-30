// Copyright © 2013-2021, EPSITEC SA, CH-1400 Yverdon-les-Bains, Switzerland
// Author: Roger VUISTINER, Maintainer: Roger VUISTINER

using System;
using System.IO;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Zou.Tasks
{
    public class CreateIndexedSubdir : Task
    {
        public string               Prefix     { get; set; }
        public string               Suffix     { get; set; }
        [Required] public string    ParentPath { get; set; }
        [Output]   public string    Path       { get; set; }

        public override bool Execute()
        {
            try
            {
                var parent = new DirectoryInfo(this.ParentPath)
                    .EnsureExists()
                    .CreateIndexedSubdirectory(out DirectoryInfo subdir, this.Prefix, this.Suffix);
                this.Path = subdir.FullName;

                this.Log.LogMessage($"Created {this.Path}");
            }
            catch (Exception e)
            {
                this.Log.LogErrorFromException(e);
            }
            return !this.Log.HasLoggedErrors;
        }
    }
}
