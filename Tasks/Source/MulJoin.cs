// Copyright © 2013-2021, EPSITEC SA, CH-1400 Yverdon-les-Bains, Switzerland
// Author: Roger VUISTINER, Maintainer: Roger VUISTINER

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Zou.Tasks
{
    public class MulJoin : Task
    {
        public ITaskItem[]   Input    { get; set; } = Array.Empty<ITaskItem>();
        public ITaskItem[]   Items    { get; set; } = Array.Empty<ITaskItem>();
        public string        ItemName { get; set; }
        [Output]
        public ITaskItem[]   Output   { get; private set; }

        public override bool            Execute()
        {
            //Debugger.Launch();

            if (this.Input.Length == 0)
            {
                this.Output = Array.Empty<ITaskItem>();
                return true;
            }
            if (this.Items.Length == 0)
            {
                this.Output = this.Input;
                return true;
            }
            this.Output = this.Input.SelectMany(project => this.CreateOutput(project)).ToArray();
            return !this.Log.HasLoggedErrors;
        }

        private IEnumerable<ITaskItem>  CreateOutput(ITaskItem input)
        {
            // create a specific output for each item
            foreach (var item in this.Items)
            {
                var output = new TaskItem(input);
                input.CopyMetadataTo(output);
                item.OverwriteCustomMetadataOf(output);
                if (!string.IsNullOrWhiteSpace(this.ItemName) && string.IsNullOrWhiteSpace(output.GetMetadata(this.ItemName)))
                {
                    output.SetMetadata(this.ItemName, item.ItemSpec);
                }
                yield return output;
            }
        }
    }
}
