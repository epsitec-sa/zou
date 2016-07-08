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
			var ids1 = doc1.Root.Elements ().Select (e => e.Attribute ("id").Value).ToArray ();
			var ids2 = doc2.Root.Elements ().Select (e => e.Attribute ("id").Value).ToArray ();
			var common = ids1.Intersect (ids2).ToArray ();
			var other2 = ids2.Except (common).ToArray ();

			if (!ids1.Except (common).IsEmpty () || !other2.IsEmpty ())
			{
				this.Changed = true;
				var elements = doc1.Root.Elements ()
					.Concat (doc2.Root.Elements ()
						.Where (e => other2.Contains (e.Attribute ("id").Value)))
					.ToArray ();
				doc1.Root.RemoveNodes ();
				doc1.Root.Add (elements);
			}
			return doc1;
		}
	}
}
