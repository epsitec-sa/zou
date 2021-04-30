// Copyright © 2013-2021, EPSITEC SA, CH-1400 Yverdon-les-Bains, Switzerland
// Author: Roger VUISTINER, Maintainer: Roger VUISTINER

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Zou.Tasks
{
    public static class DebuggerEx
    {
        public static void WaitAttached(TaskLoggingHelper log)
        {
            log.LogMessage(MessageImportance.High, $"Attach to MSBuild (PID = {System.Diagnostics.Process.GetCurrentProcess().Id})");
            while (!System.Diagnostics.Debugger.IsAttached) { }
        }
    }
}
