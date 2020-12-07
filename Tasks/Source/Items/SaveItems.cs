// Copyright © 2013-2020, EPSITEC SA, CH-1400 Yverdon-les-Bains, Switzerland
// Author: Roger VUISTINER, Maintainer: Roger VUISTINER

using Bcx.Linq;
using Microsoft.Build.Framework;
using System;
using System.Linq;

namespace Zou.Tasks
{
	public class SaveItems : ItemsTask
	{
        public string			        Metadata       { get; set; }
        public bool				        KeepDuplicates { get; set; }
        public bool				        Overwrite      { get; set; }
		[Required] public ITaskItem[]   Items          { get; set; }
        [Output]   public ITaskItem[]	OldItems       { get; set; }
        [Output]   public ITaskItem[]	NewItems       { get; set; }

        public override bool    Execute()
		{
			var inputMemos = this.Items.Select (item => ItemMemo.FromTaskItem (item, this.RelativeTo, this.Metadata)).ToArray ();
			var oldMemos   = ItemMemo.Parse (this.ReadAllLines ()).ToArray ();
			var newMemos   = this.MergeMemos (oldMemos, inputMemos);

			System.IO.File.WriteAllLines (this.File, newMemos.Serialize ().ToArray ());

			this.OldItems  = oldMemos.Select (m => m.ToTaskItem (this.RelativeTo)).ToArray ();
			this.NewItems  = newMemos.Select (m => m.ToTaskItem (this.RelativeTo)).ToArray ();

			return !this.Log.HasLoggedErrors;
		}

		private ItemMemo[]		MergeMemos(ItemMemo[] previousMemos, ItemMemo[] inputMemos)
		{
			if (this.Overwrite)
			{
				return inputMemos;
			}
			else
			{
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
