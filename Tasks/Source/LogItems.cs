using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.BuildEngine;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Zou
{
	public class LogItems : Task
	{
		public override bool Execute()
		{
			this.Items.ForEach (item => item.Log (this.Title, this.Log));
			return true;
		}

		public string Title
		{
			get; set;
		}
		[Required]
		public ITaskItem[] Items
		{
			get;
			set;
		}
	}
	internal static partial class Mixins
	{
		public static void Log(this ITaskItem item, string title, TaskLoggingHelper log)
		{
			var header = string.IsNullOrEmpty (title) ? $"{item.ItemSpec}:" : $"{title} [{item.ItemSpec}]";

			var buildItem = new BuildItem ("Item", item);
			var names = buildItem
				.CustomMetadataNames
				.Cast<string> ()
				.Where (name => name != "OriginalItemSpec")
				.OrderBy(name => name);
			if (names.IsEmpty ())
			{
				log.LogMessage (MessageImportance.Normal, header);
			}
			else
			{
				var maxNameLength = names.Max (name => name.Length);
				var lines = names
					.Select (name => $"  {name.PadRight (maxNameLength)} = {buildItem.GetMetadata (name)}")
					.StartWith ($"{header}:");

				log.LogMessage (MessageImportance.Normal, string.Join ("\n", lines));
			}
		}
	}
}
