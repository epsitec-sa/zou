using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.BuildEngine;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Epsitec.Zou
{
	public class LoadItems : ItemsTask
	{
		[Output]
		public ITaskItem[]				Items
		{
			get;
			set;
		}
		public override bool			Execute()
		{
			this.Items = ItemMemo
				.Parse (this.ReadAllLines ())
				.Select(m => m.ToTaskItem(this.RelativeTo))
				.ToArray ();

			return !this.Log.HasLoggedErrors;
		}
	}
}
