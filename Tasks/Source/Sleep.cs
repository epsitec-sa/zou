// Copyright © 2013-2020, EPSITEC SA, CH-1400 Yverdon-les-Bains, Switzerland
// Author: Roger VUISTINER, Maintainer: Roger VUISTINER

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Zou.Tasks
{
    public class Sleep : Task
	{
		[Required] public int Timeout { get; set; }

        public override bool Execute()
		{
			System.Threading.Thread.Sleep (this.Timeout);
			return !this.Log.HasLoggedErrors;
		}
	}
}
