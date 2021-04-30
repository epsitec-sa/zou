// Copyright © 2013-2021, EPSITEC SA, CH-1400 Yverdon-les-Bains, Switzerland
// Author: Roger VUISTINER, Maintainer: Roger VUISTINER

using System;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Zou.Tasks
{
    public class SetEnvVar : Task
    {
        [Required] public string Variable { get; set; }
        [Required] public string Value    { get; set; }

        public override bool Execute()
        {
            Environment.SetEnvironmentVariable(this.Variable, this.Value);
            return true;
        }
    }
}
