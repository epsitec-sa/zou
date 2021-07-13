// Copyright © 2013-2021, EPSITEC SA, CH-1400 Yverdon-les-Bains, Switzerland
// Author: Roger VUISTINER, Maintainer: Roger VUISTINER

using Bcx.IO;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Zou.Tasks
{
    public class GetRealPath : Task
    {
        [Required] public string Path     { get; set; }
        [Output]   public string RealPath { get; private set; }
        public override bool Execute()
        {
            this.RealPath = PathEx.GetRealPath(this.Path);
            return !this.Log.HasLoggedErrors;
        }
    }
}
