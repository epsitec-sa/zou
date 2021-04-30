// Copyright © 2013-2021, EPSITEC SA, CH-1400 Yverdon-les-Bains, Switzerland
// Author: Roger VUISTINER, Maintainer: Roger VUISTINER

using System.IO;
using System.Linq;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Zou.Tasks
{
    public class AddFileInfo : Task
    {
        [Required] public ITaskItem[]   Files       { get; set; }
        [Output]   public ITaskItem[]	OutputFiles { get; private set; }

        public override bool	Execute()
        {
            this.OutputFiles = this.Files.Select (file => this.AddInfo (file)).ToArray ();
            return !this.Log.HasLoggedErrors;
        }

        private ITaskItem		AddInfo(ITaskItem fileItem)
        {
            var file = new TaskItem (fileItem);
            var fileInfo = new FileInfo (file.ItemSpec);
            file.SetMetadata ("Exists", fileInfo.Exists.ToString ());
            if (fileInfo.Exists)
            {
                file.SetMetadata ("Length", fileInfo.Length.ToString ());
                file.SetMetadata ("IsReadOnly", fileInfo.IsReadOnly.ToString ());
            }
            else
            {
                file.SetMetadata ("Length", "0");
                file.SetMetadata ("IsReadOnly", string.Empty);
            }
            return file;
        }
    }
}
