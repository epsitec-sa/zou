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
		[Output]
		public ITaskItem[]		OutputItems
		{
			get;
			set;
		}
		public override bool	Execute()
		{
			var inputMemos   = this.Items.Select (item => ItemMemo.FromTaskItem (item, this.RelativeTo, this.Metadata)).ToArray ();
			var outputMemos  = this.GetOuptutMemos (inputMemos);
			this.OutputItems = outputMemos.Select (m => m.ToTaskItem (this.RelativeTo)).ToArray ();

			System.IO.File.WriteAllLines (this.File, outputMemos.Serialize ().ToArray ());

			return !this.Log.HasLoggedErrors;
		}

		private ItemMemo[]		GetOuptutMemos(ItemMemo[] inputMemos)
		{
			if (this.Overwrite)
			{
				return inputMemos;
			}
			else
			{
				var previousMemos = ItemMemo.Parse (this.ReadAllLines ()).ToArray ();
				if (this.KeepDuplicates)
				{
					return inputMemos.Concat (previousMemos).ToArray ();
				}
				else
				{
					var commonMemos = inputMemos.Join (previousMemos, c => c.Id, p => p.Id, (c, p) => c.MergeMetadata (p), StringComparer.OrdinalIgnoreCase);
					var newMemos    = inputMemos.Except (commonMemos, m => m.Id, StringComparer.OrdinalIgnoreCase);
					var oldMemos    = previousMemos.Except (commonMemos, m => m.Id, StringComparer.OrdinalIgnoreCase);

					return newMemos.Concat (commonMemos).Concat (oldMemos).ToArray ();
				}
			}
		}
	}
}
