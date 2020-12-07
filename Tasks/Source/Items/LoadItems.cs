// Copyright © 2013-2020, EPSITEC SA, CH-1400 Yverdon-les-Bains, Switzerland
// Author: Roger VUISTINER, Maintainer: Roger VUISTINER

using Microsoft.Build.Framework;
using System.Linq;

namespace Zou.Tasks
{
	public class LoadItems : ItemsTask
	{
		[Output] public ITaskItem[] Items { get; set; }

        public override bool Execute()
		{
			this.Items = ItemMemo
				.Parse (this.ReadAllLines ())
				.Select(m => m.ToTaskItem(this.RelativeTo))
				.ToArray ();

			return !this.Log.HasLoggedErrors;
		}
	}
}
