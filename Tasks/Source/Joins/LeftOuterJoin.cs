// Copyright © 2013-2021, EPSITEC SA, CH-1400 Yverdon-les-Bains, Switzerland
// Author: Roger VUISTINER, Maintainer: Roger VUISTINER

using System.Collections.Generic;
using System.Linq;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Zou.Tasks
{
    public class LeftOuterJoin : JoinTask
	{
		protected override IEnumerable<ITaskItem> DoJoin()
		{
			var leftOuterQuery =
				from outer in this.Outer
				join inner in this.Inner on outer.JoinMetadata (this.Keys) equals inner.JoinMetadata (this.Keys) into innerGroup
				from item in innerGroup.DefaultIfEmpty (null)
				select new
				{
					Outer = outer,
					Inner = item
				};

			var modifiers = Modifier.Parse (this.Modifiers).ToArray ();

			foreach (var pair in leftOuterQuery)
			{
				var item = new TaskItem (pair.Outer);
				foreach (var modifier in modifiers)
				{
					item.SetMetadata (modifier.LValue, pair.Inner?.GetMetadata (modifier.RValue) ?? string.Empty);
					yield return item;
				}
			}
		}
	}
}
