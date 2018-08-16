using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;

namespace Epsitec.Zou
{
	public class Lock : Task
	{
		[Required]
		public string						Name
		{
			get;
			set;
		}
		public bool							Global
		{
			get;
			set;
		}
		public int							Timeout
		{
			get;
			set;
		}
		public override bool				Execute()
		{
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Unix does not support named semaphores (PlatformNotSupportedException)
                return true;
            }
			try
			{
				string semName = Lock.GetName (this.Name, this.Global);
				var sem = new Semaphore (1, 1, semName, out bool semCreated);

				// Register the semaphore with the build engine:
				// - to keep it alive until the end of the build (avoid GC)
				// - optionnally to make it available for Unlock task.
				this.RegisterSemaphore (semName, sem);

				this.Log.LogMessageFromText ($"Lock -> waiting semaphore '{semName}', created = {semCreated}.", MessageImportance.Normal);
				var hasSem = sem.WaitOne (this.TimeoutInternal);
				if (hasSem)
				{
					this.Log.LogMessageFromText ($"Lock -> entered semaphore '{semName}', created = {semCreated}.", MessageImportance.Normal);
				}
				else
				{
					throw new TimeoutException ($"Lock -> timeout waiting for semaphore '{semName}'.");
				}


			}
			catch (Exception e)
			{
				this.Log.LogErrorFromException (e);
			}
			return !this.Log.HasLoggedErrors;
		}

		private int							TimeoutInternal => this.Timeout == 0 ? System.Threading.Timeout.Infinite : this.Timeout;
		private void						RegisterSemaphore(string semName, Semaphore sem)
		{
			this.BuildEngine4.RegisterTaskObject (semName, sem, RegisteredTaskObjectLifetime.AppDomain, false);
		}

		internal static string				GetName(string name, bool global)
		{
			// Replace consecutive non-alphanumeric characters with underscore.
			var semName = Regex.Replace (name, @"\W+", "_", RegexOptions.CultureInvariant).ToLowerInvariant ();
			if (global)
			{
				return "Global\\" + semName;
			}
			return semName;
		}
	}
}
