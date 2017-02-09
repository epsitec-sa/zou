using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Epsitec.Zou
{
	public class MergePackagesConfig : Task
	{
		[Required]
		public ITaskItem[] MergeFiles
		{
			get;
			set;
		}
		[Required]
		public string IntoFile
		{
			get;
			set;
		}
		[Output]
		public bool Changed
		{
			get; set;
		}
		public override bool				Execute()
		{
			try
			{
				var mergePathes = this.MergeFiles.Select (item => item.GetMetadata ("Identity")).ToArray ();
				this.Log.LogMessage ($"Merging {string.Join(", ", mergePathes)} into {this.IntoFile}");
				var merged = mergePathes
					.Select (path => XDocument.Load (path))
					.Aggregate(XDocument.Load (this.IntoFile), (doc1, doc2) => this.Merge(doc1, doc2));
				if (this.Changed)
				{
					// order elements by id...
					var elements = merged.Root.Elements ()
						.OrderBy (e => e.Attribute ("id").Value, StringComparer.OrdinalIgnoreCase).ToArray ();
					merged.Root.RemoveNodes ();
					merged.Root.Add (elements);
					// ... and save
					merged.Save (this.IntoFile);
				}
			}
			catch (Exception e)
			{
				this.Log.LogErrorFromException (e);
			}
			return !this.Log.HasLoggedErrors;
		}

		private XDocument Merge(XDocument doc1, XDocument doc2)
		{
			var count0 = doc1.Root.Elements ().Count ();
			doc1.Root.Add (
				doc2.Root.Elements()
					.Except(doc1.Root.Elements(), e => e.Attribute ("id").Value, StringComparer.OrdinalIgnoreCase));

			if (count0 != doc1.Root.Elements ().Count ())
			{
				this.Changed = true;
			}
			return doc1;
		}
	}
}
