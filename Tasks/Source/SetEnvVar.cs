using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;

namespace Epsitec.Zou
{
    public class SetEnvVar : Task
    {
        [Required]
        public string Variable { get; set; }

        [Required]
        public string Value { get; set; }

        public override bool Execute()
        {
            Environment.SetEnvironmentVariable(this.Variable, this.Value);
            return true;
        }
    }
}