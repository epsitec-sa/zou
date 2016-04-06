using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.BuildEngine;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Zou
{
	public class AddBuildOptions : Task
	{
		public override bool			Execute()
		{
			this.ProjectsOutput = this.Projects.Select (project => project.AddBuildOptions ()).ToArray();
			return true;
		}
		[Required]
		public ITaskItem[]				Projects
		{
			get;
			set;
		}
		[Output]
		public ITaskItem[]				ProjectsOutput
		{
			get; private set;
		}
	}
	internal static partial class Mixins
	{
		public static ITaskItem				AddBuildOptions(this ITaskItem project)
		{
			Mixins.PreprocessBuildOptions (project);

			var taskItem = new TaskItem (project);
			var buildItem = new BuildItem ("Project", project);
			var names = buildItem.CustomMetadataNames.Cast<string> ();


			// Add Properties metadata
			var properties = names
				.Where (name => Mixins.IsBuildProperty (name))
				.Select (name => Tuple.Create (name, buildItem.GetMetadata (name)))
				.OrderBy (a => a, BuildPropertyComparer.Default)
				.Select (a => $"{a.Item1}={a.Item2}")
				.ToArray ();

			var propertiesValue = string.Join (";", properties);
			if (propertiesValue.EndsWith ("\\"))
			{
				propertiesValue += ";_=_";
			}
			taskItem.SetMetadata ("Properties", propertiesValue);

			return taskItem;
		}

		private static IEnumerable<string>	NonBuildProperties
		{
			get
			{
				yield return "BuildInParallel";
				yield return "Targets";
			}
		}
		private static bool					IsBuildProperty(string name) => !Mixins.NonBuildProperties.Contains (name);
		private static void					PreprocessBuildOptions(ITaskItem project)
		{
			// Preprocess the 'Any CPU' platform property.
			// If the item spec is a .sln use 'Any CPU' (with a space).
			// If the item spec is a .csproj use 'AnyCPU' (without a space).
			var platform = project.GetMetadata ("Platform");
			if (platform != null)
			{
				platform = platform.ToLowerInvariant ();
				if (platform == "any cpu" || platform == "anycpu")
				{
					var extension = Path.GetExtension (project.ItemSpec).ToLowerInvariant ();
					if (extension == ".sln")
					{
						project.SetMetadata ("Platform", "Any CPU");
					}
					else
					{
						project.SetMetadata ("Platform", "AnyCPU");
					}
				}
			}
		}

		private class BuildPropertyComparer : IComparer<Tuple<string, string>>
		{
			public static readonly BuildPropertyComparer Default = new BuildPropertyComparer ();

			public int Compare(Tuple<string, string> x, Tuple<string, string> y)
			{
				var xEndsWithBackslash = x.Item2.EndsWith ("\\");
				var yEndsWithBackslash = y.Item2.EndsWith ("\\");
				if (xEndsWithBackslash)
				{
					return yEndsWithBackslash ? string.Compare (x.Item1, y.Item1) : -1;
				}
				else if (yEndsWithBackslash)
				{
					return xEndsWithBackslash ? string.Compare (x.Item1, y.Item1) : 1;
				}
				else
				{
					return string.Compare (x.Item1, y.Item1);
				}
			}
		}
	}
}
