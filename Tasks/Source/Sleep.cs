using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Epsitec.Zou
{
	public class Sleep : Task
	{
		[Required]
		public int Timeout
		{
			get;
			set;
		}
		public override bool Execute()
		{
			System.Threading.Thread.Sleep (this.Timeout);
			return !this.Log.HasLoggedErrors;
		}
	}
}
