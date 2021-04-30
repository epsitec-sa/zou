// Copyright © 2013-2021, EPSITEC SA, CH-1400 Yverdon-les-Bains, Switzerland
// Author: Roger VUISTINER, Maintainer: Roger VUISTINER

using System;
using System.Runtime.InteropServices;
using System.Threading;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Zou.Tasks
{
    public class Unlock : Task
    {
        public bool                 Global { get; set; }
        [Required] public string    Name   { get; set; }

        public override bool        Execute()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Unix does not support named semaphores (PlatformNotSupportedException)
                return true;
            }
            try
            {
                var semName = Lock.GetName(this.Name, this.Global);
                var sem = this.GetRegisteredSemaphore(semName);
                if (sem == null)
                {
                    this.Log.LogError($"Unlock -> semaphore '{semName}' not registered.");
                }
                else
                {
                    this.Log.LogMessageFromText($"Unlock -> released semaphore '{semName}'.", MessageImportance.Normal);
                    sem.Release();
                }
            }
            catch (Exception e)
            {
                this.Log.LogErrorFromException(e);
            }
            return !this.Log.HasLoggedErrors;
        }

        private Semaphore           GetRegisteredSemaphore(string name)
        {
            return this.BuildEngine4.GetRegisteredTaskObject(name, RegisteredTaskObjectLifetime.AppDomain) as Semaphore;
        }
    }
}
