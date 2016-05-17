using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Epsitec.Zou
{
	public class Unlock : Task
	{
		[Required]
		public string Name
		{
			get;
			set;
		}
		public bool Global
		{
			get;
			set;
		}
		public override bool Execute()
		{
			// Replace consecutive non-alphanumeric characters with underscore.
			string semName = Lock.GetName (this.Name, this.Global);
			try
			{
				Semaphore
					.OpenExisting (semName, SemaphoreRights.Modify | SemaphoreRights.Synchronize)
					.Release ();

				this.Log.LogMessageFromText ($"[{Process.GetCurrentProcess ().Id}] Unlock -> released sem: {semName}", MessageImportance.Normal);
			}
			catch (Exception e)
			{
				this.Log.LogErrorFromException (e);
			}
			return !this.Log.HasLoggedErrors;
		}
	}
}
