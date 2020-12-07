// Copyright © 2013-2020, EPSITEC SA, CH-1400 Yverdon-les-Bains, Switzerland
// Author: Roger VUISTINER, Maintainer: Roger VUISTINER

using Bcx.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace Zou.Tasks
{
	public class GroupJoin : JoinTask
	{
		protected override IEnumerable<ITaskItem> DoJoin()
		{
			var groupQuery =
				from outer in this.Outer
				join inner in this.Inner on outer.JoinMetadata (this.Keys) equals inner.JoinMetadata (this.Keys) into innerGroup
				select new
				{
					Outer = outer,
					Group = innerGroup
				};

			var modifiers = Modifier.Parse (this.Modifiers).ToArray ();

			foreach (var pair in groupQuery)
			{
				var item = new TaskItem (pair.Outer);
				foreach (var modifier in modifiers)
				{
					item.SetMetadata (modifier.LValue, pair.Group?.Select(inner => inner.GetMetadata (modifier.RValue)).Join(";"));
				}
				yield return item;
			}
		}
	}
}
