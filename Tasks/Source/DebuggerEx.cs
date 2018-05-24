using System;
using System.Threading;

namespace Epsitec.Zou
{
    public static class DebuggerEx
    {
        public static void WaitAttached()
        {
            Console.WriteLine($"Attach to MSBuild (PID = {System.Diagnostics.Process.GetCurrentProcess().Id})");
            while (!System.Diagnostics.Debugger.IsAttached)
            {
                Thread.Sleep(100);
            }
        }
    }
}
