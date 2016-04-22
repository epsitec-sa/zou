using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.BuildEngine;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Epsitec.Zou
{
	public class AddFileInfo : Task
	{
		public override bool Execute()
		{
			this.OutputFiles = this.Files.Select (file => this.AddInfo (file)).ToArray ();
			return true;
		}
		[Required]
		public ITaskItem[] Files
		{
			get; set;
		}
		[Output]
		public ITaskItem[] OutputFiles
		{
			get;
			private set;
		}
		private ITaskItem AddInfo(ITaskItem fileItem)
		{
			var file = new TaskItem (fileItem);
			var fileInfo = new FileInfo (file.ItemSpec);
			file.SetMetadata ("Length", fileInfo.Length.ToString ());
			file.SetMetadata ("IsReadOnly", fileInfo.IsReadOnly.ToString ());
			return file;
		}
	}
}
