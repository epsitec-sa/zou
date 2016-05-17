using System;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
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
		public bool Global
		{
			get;
			set;
		}
		public int Timeout
		{
			get;
			set;
		}
		public override bool Execute()
		{
			try
			{
				var timeout = this.Timeout == 0 ? System.Threading.Timeout.Infinite : this.Timeout;
				string semName = Lock.GetName (this.Name, this.Global);
				bool semCreated;
				var sem = new Semaphore (1, 1, semName, out semCreated, Lock.GetSecurity (this.Global));
				this.Log.LogMessageFromText ($"Lock -> waiting sem: {semName}, semCreated = {semCreated}", MessageImportance.Normal);
				var hasSem = sem.WaitOne (timeout, false);
				if (!hasSem)
				{
					throw new TimeoutException ($"Lock -> timeout waiting for {semName}");
				}

				// Register the semaphore to keep it alive until the end of the build (avoid GC).
				this.BuildEngine4.RegisterTaskObject (semName, sem, RegisteredTaskObjectLifetime.Build, false);

				this.Log.LogMessageFromText ($"Lock -> entered sem: {semName}, semCreated = {semCreated}", MessageImportance.Normal);
			}
			catch (Exception e)
			{
				this.Log.LogErrorFromException (e);
			}
			return !this.Log.HasLoggedErrors;
		}

		internal static string GetName(string name, bool global)
		{
			// Replace consecutive non-alphanumeric characters with underscore.
			var semName = Regex.Replace (name.ToLowerInvariant (), @"[^\w]+", "_", RegexOptions.CultureInvariant);
			if (global)
			{
				return "Global\\" + semName;
			}
			return semName;
		}
		private static SemaphoreSecurity GetSecurity(bool global)
		{
			if (global)
			{
				var worldId = new SecurityIdentifier (WellKnownSidType.WorldSid, null);
				var semAccessRule = new SemaphoreAccessRule (worldId, SemaphoreRights.FullControl, AccessControlType.Allow);
				var semSecurity = new SemaphoreSecurity ();
				semSecurity.AddAccessRule (semAccessRule);
				return semSecurity;
			}
			return null;
		}
	}
}
