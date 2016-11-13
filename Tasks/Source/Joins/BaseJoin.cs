using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.BuildEngine;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Epsitec.Zou
{
	public abstract class JoinTask : Task
	{
		[Required]
		public ITaskItem[]		Outer
		{
			get;
			set;
		}
		[Required]
		public ITaskItem[]		Inner
		{
			get;
			set;
		}
		[Required]
		public string			Keys
		{
			get; set;
		}
		[Required]
		public string			Modifiers
		{
			get; set;
		}
		[Output]
		public ITaskItem[]		Result
		{
			get;
			private set;
		}
		public override bool	Execute()
		{
			this.Result = this.DoJoin ().ToArray ();
			return !this.Log.HasLoggedErrors;
		}

		protected abstract IEnumerable<ITaskItem> DoJoin();

		protected class Modifier
		{
			public static IEnumerable<Modifier> Parse(string value)
			{
				return value
					.Split (';')
					.Select (item => new Modifier (item.Split ('=')));
			}

			public Modifier(string[] v)
			{
				this.LValue = v[0];
				this.RValue = v[1];
			}
			public string LValue
			{
				get;
			}
			public string RValue
			{
				get;
			}
		}
	}
}
