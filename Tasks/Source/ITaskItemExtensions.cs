using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Epsitec.Zou
{
	public static class ITaskItemExtensions
	{
		public static IEnumerable<string> GetMetadata(this ITaskItem self, IEnumerable<string> keys)
		{
			return keys.Select (k => self.GetMetadata (k));
		}
		public static string JoinMetadata(this ITaskItem self, string keys)
		{
			return string.Join(";", keys.Split(';').Select (k => self.GetMetadata (k)));
		}
	}
}
