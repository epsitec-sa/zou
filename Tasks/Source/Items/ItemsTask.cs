// Copyright © 2013-2021, EPSITEC SA, CH-1400 Yverdon-les-Bains, Switzerland
// Author: Roger VUISTINER, Maintainer: Roger VUISTINER

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Zou.Tasks
{
    public abstract class ItemsTask : Task
    {
        public string               RelativeTo { get; set; }
        [Required] public string    File       { get; set; }

        protected string[] ReadAllLines()
        {
            return System.IO.File.Exists (this.File)
                ? System.IO.File.ReadAllLines (this.File)
                : new string[0];
        }
    }
}
