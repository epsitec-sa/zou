using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.BuildEngine;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Epsitec.Zou
{
	public class LogItems : Task
	{
		public string			Title
		{
			get; set;
		}
		public bool				AllMetadata
		{
			get; set;
		}
		[Required]				
		public ITaskItem[]		Items
		{
			get;
			set;
		}
		public override bool	Execute()
		{
			this.Items.ForEach (item => this.LogItem (item));
			return !this.Log.HasLoggedErrors;
		}

		private void			LogItem(ITaskItem item)
		{
			var header = string.IsNullOrEmpty (this.Title) ? $"{item.ItemSpec}:" : $"{this.Title} [{item.ItemSpec}]";

			if (this.AllMetadata)
			{
				this.LogMetadata (item.MetadataNames, header, item.GetMetadata);
			}
			else
			{
				var buildItem = new BuildItem ("Item", item);
				this.LogMetadata (buildItem.CustomMetadataNames, header, item.GetMetadata);
			}
		}
		private void			LogMetadata(System.Collections.ICollection names, string header, Func<string, string> getMetaData)
		{
			this.LogMetadata (names.Cast<string> (), header, getMetaData);
		}
		private void			LogMetadata(IEnumerable<string> names, string header, Func<string, string> getMetaData)
		{
			if (names.IsEmpty ())
			{
				this.Log.LogMessage (MessageImportance.Normal, header);
			}
			else
			{
				var maxNameLength = names.Max (name => name.Length);
				var lines = names
					.Where (name => name != "OriginalItemSpec")
					.OrderBy (name => name)
					.Select (name => $"  {name.PadRight (maxNameLength)} = {getMetaData (name)}")
					.StartWith ($"{header}:");

				this.Log.LogMessage (MessageImportance.Normal, string.Join ("\n", lines));
			}
		}
	}
}
