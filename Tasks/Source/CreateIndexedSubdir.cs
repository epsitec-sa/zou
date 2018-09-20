using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;

namespace Epsitec.Zou
{
    public class CreateIndexedSubdir : Task
	{
        [Required]
        public string ParentPath
        {
            get;
            set;
        }
        public string Prefix
        {
            get;
            set;
        }
        public string Suffix
        {
            get;
            set;
        }
        [Output]
		public string Path
		{
			get; set;
		}
		public override bool				Execute()
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
				this.Log.LogErrorFromException (e);
			}
			return !this.Log.HasLoggedErrors;
		}
	}
}
