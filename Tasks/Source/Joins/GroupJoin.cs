using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.BuildEngine;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Epsitec.Zou
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
					yield return item;
				}
			}
		}
	}
}
