using System;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Epsitec.Zou
{
    public static class DebuggerEx
    {
        public static void WaitAttached(TaskLoggingHelper log)
        {
            log.LogMessage(MessageImportance.High, $"Attach to MSBuild (PID = {System.Diagnostics.Process.GetCurrentProcess().Id})");
            while (!System.Diagnostics.Debugger.IsAttached) ;
        }
    }
}
