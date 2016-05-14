using System;
using System.Collections.Generic;
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
		public override bool Execute()
		{
			try
			{
				// Replace consecutive non-alphanumeric characters with underscore.
				var mutexName = Regex.Replace (this.Name.ToLowerInvariant (), @"[^\w]+", "_", RegexOptions.CultureInvariant);
				Mutex mutex;
				if (Mutex.TryOpenExisting (mutexName, MutexRights.Modify | MutexRights.Synchronize, out mutex))
				{
					mutex.ReleaseMutex ();
					this.Log.LogMessageFromText ($"Unlock -> exited mutex: {mutexName}", MessageImportance.Normal);
				}
				else
				{
					this.Log.LogMessageFromText ($"Unlock -> unable to open mutex: {mutexName}", MessageImportance.Normal);
				}
			}
			catch (Exception e)
			{
				this.Log.LogErrorFromException (e);
			}
			return !this.Log.HasLoggedErrors;
		}
	}
}
