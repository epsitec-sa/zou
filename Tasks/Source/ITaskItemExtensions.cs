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
		public static IEnumerable<string> CustomMetadataNames(this ITaskItem self)
		{
			return self.MetadataNames.Cast<string>().Except(StandardMetadataNames);
		}

		private static IEnumerable<string> StandardMetadataNames
		{
			get
			{
				yield return "FullPath";
				yield return "RootDir";
				yield return "Filename";
				yield return "Extension";
				yield return "RelativeDir";
				yield return "Directory";
				yield return "RecursiveDir";
				yield return "Identity";
				yield return "ModifiedTime";
				yield return "CreatedTime";
				yield return "AccessedTime";
				yield return "DefiningProjectFullPath";
				yield return "DefiningProjectDirectory";
				yield return "DefiningProjectName";
				yield return "DefiningProjectExtension";
			}
		}
	}
}
