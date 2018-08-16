using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Epsitec.Zou
{
	public class Unlock : Task
	{
		[Required]
		public string			Name
		{
			get;
			set;
		}
		public bool				Global
		{
			get;
			set;
		}
		public override bool	Execute()
		{
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Unix does not support named semaphores (PlatformNotSupportedException)
                return true;
            }
            try
            {
				var semName = Lock.GetName (this.Name, this.Global);
				var sem = this.GetRegisteredSemaphore (semName);
				if (sem == null)
				{
					this.Log.LogError ($"Unlock -> semaphore '{semName}' not registered.");
				}
				else
				{
					this.Log.LogMessageFromText ($"Unlock -> released semaphore '{semName}'.", MessageImportance.Normal);
					sem.Release ();
				}
			}
			catch (Exception e)
			{
				this.Log.LogErrorFromException (e);
			}
			return !this.Log.HasLoggedErrors;
		}

		private Semaphore		GetRegisteredSemaphore(string name) => this.BuildEngine4.GetRegisteredTaskObject (name, RegisteredTaskObjectLifetime.AppDomain) as Semaphore;
	}
}
