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
				// Replace consecutive non-alphanumeric characters with underscore.
				var mutexName = Regex.Replace (this.Name.ToLowerInvariant (), @"[^\w]+", "_", RegexOptions.CultureInvariant);
				bool mutexCreated;
				var mutex = new Mutex (false, mutexName, out mutexCreated);
				try
				{
					this.Log.LogMessageFromText ($"Lock -> waiting mutex: {mutexName}, mutexCreated = {mutexCreated}", MessageImportance.Normal);
					if (!mutex.WaitOne (timeout, false))
					{
						throw new TimeoutException ($"Lock -> timeout waiting for {mutexName}");
					}
					this.Log.LogMessageFromText ($"Lock -> entered mutex: {mutexName}, mutexCreated = {mutexCreated}", MessageImportance.Normal);
				}
				catch (AbandonedMutexException)
				{
					this.Log.LogMessageFromText ($"Lock -> entered abandoned mutex: {mutexName}, mutexCreated = {mutexCreated}", MessageImportance.High);
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
