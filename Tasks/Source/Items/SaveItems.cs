using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.BuildEngine;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Epsitec.Zou
{
	public class SaveItems : ItemsTask
	{
		[Required]
		public ITaskItem[]		Items
		{
			get;
			set;
		}
		public string			Metadata
		{
			get;
			set;
		}
		public bool				KeepDuplicates
		{
			get;
			set;
		}
		public bool				Overwrite
		{
			get;
			set;
		}
		public override bool	Execute()
		{
			var currentMemos = this.Items
				.Select (item => ItemMemo.FromTaskItem (item, this.RelativeTo, this.Metadata))
				.ToArray ();

			if (this.Overwrite)
			{
				System.IO.File.WriteAllLines (this.File, currentMemos.Serialize ().ToArray ());
			}
			else if (this.KeepDuplicates)
			{
				System.IO.File.AppendAllLines (this.File, currentMemos.Serialize ().ToArray ());
			}
			else
			{
				var previousMemos = ItemMemo
					.Parse (this.ReadAllLines ())
					.ToArray ();

				var commonMemos = currentMemos.Join    (previousMemos, c => c.Id, p => p.Id, (c, p) => c.MergeMetadata (p), StringComparer.OrdinalIgnoreCase);
				var newMemos    = currentMemos.Except  (commonMemos, m => m.Id, StringComparer.OrdinalIgnoreCase);
				var oldMemos    = previousMemos.Except (commonMemos, m => m.Id, StringComparer.OrdinalIgnoreCase);

				System.IO.File.WriteAllLines (this.File, newMemos.Concat (commonMemos).Concat (oldMemos).Serialize ().ToArray ());

			}
			return !this.Log.HasLoggedErrors;
		}

	}
}
