using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.BuildEngine;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Epsitec.Zou
{
	public class Join : Task
	{
		[Required]
		public ITaskItem[]				Outer
		{
			get;
			set;
		}
		[Required]
		public ITaskItem[]				Inner
		{
			get;
			set;
		}
		[Required]
		public string					Key
		{
			get; set;
		}
		[Required]
		public string					Modifiers
		{
			get; set;
		}
		[Output]
		public ITaskItem[]				Result
		{
			get;
			private set;
		}
		public override bool			Execute()
		{
			this.Result = this.LeftOuterJoin ().ToArray ();
			return !this.Log.HasLoggedErrors;
		}

		private IEnumerable<ITaskItem>	LeftOuterJoin()
		{
			var leftOuterQuery =
				from outer in this.Outer
				join inner in this.Inner on outer.GetMetadata (this.Key) equals inner.GetMetadata (this.Key) into outerGroup
				from item in outerGroup.DefaultIfEmpty (null)
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

		private class Modifier
		{
			public static IEnumerable<Modifier>	Parse(string value)
			{
				return value
					.Split (';')
					.Select (item => new Modifier (item.Split ('=')));
			}

			public								Modifier(string[] v)
			{
				this.LValue = v[0];
				this.RValue = v[1];
			}
			public string						LValue
			{
				get;
			}
			public string						RValue
			{
				get;
			}
		}
	}
}
