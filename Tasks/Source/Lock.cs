using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Epsitec.Zou
{
	public class Lock : Task
	{
		[Required]
		public string Name
		{
			get;
			set;
		}
		public override bool Execute()
		{
			try
			{
				// Replace consecutive non-alphanumeric characters with underscore.
				var mutexName = Regex.Replace (this.Name, @"[^\w]+", "_", RegexOptions.CultureInvariant);
				bool mutexCreated;
				var mutex = new Mutex (true, mutexName, out mutexCreated);
				if (!mutexCreated)
				{
					mutex.WaitOne ();
				}
			}
			catch(Exception e)
			{
				this.Log.LogErrorFromException (e);
			}
			return !this.Log.HasLoggedErrors;
		}
	}
}
