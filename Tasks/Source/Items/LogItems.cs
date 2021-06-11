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
    public class LogItems : Task
    {
        public string                   Title       { get; set; }
        public bool                     AllMetadata { get; set; }
        public string                   Importance  { get; set; }
        [Required] public ITaskItem[]   Items       { get; set; }

        public override bool Execute()
        {
            //Debugger.Launch();

            this.Items.ForEach(item => this.LogItem(item));
            return !this.Log.HasLoggedErrors;
        }

        private void LogItem(ITaskItem item)
        {
            var header = string.IsNullOrEmpty(this.Title) ? $"{item.ItemSpec}:" : $"{this.Title} [{item.ItemSpec}]";

            if (this.AllMetadata)
            {
                this.LogMetadata(item.MetadataNames, header, item.GetMetadata);
            }
            else
            {
                this.LogMetadata(item.CustomMetadataNames(), header, item.GetMetadata);
            }
        }
        private void LogMetadata(System.Collections.ICollection names, string header, Func<string, string> getMetaData) => this.LogMetadata(names.Cast<string>(), header, getMetaData);
        private void LogMetadata(IEnumerable<string> names, string header, Func<string, string> getMetaData)
        {
            var importance = string.IsNullOrEmpty(this.Importance) ? MessageImportance.High : (MessageImportance) Enum.Parse(typeof(MessageImportance), this.Importance, true);

            if (names.IsEmpty())
            {
                this.Log.LogMessage(importance, header);
            }
            else
            {
                var maxNameLength = names.Max(name => name.Length);
                var lines = names
                  .Where(name => name != "OriginalItemSpec")
                  .OrderBy(name => name)
                  .Select(name => $"  {name.PadRight(maxNameLength)} = {getMetaData(name)}")
                  .StartWith($"{header}:");

                this.Log.LogMessage(importance, string.Join("\n", lines));
            }
        }
    }
}
