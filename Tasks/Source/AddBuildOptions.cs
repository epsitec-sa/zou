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
	public enum Language
	{
		None,
		Cpp,
		CSharp
	}

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
		public static ITaskItem				AddBuildOptions(this ITaskItem projectItem)
		{
			var project = new TaskItem (projectItem);
			Mixins.PreprocessOptions (project);

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
			project.SetMetadata ("Properties", propertiesValue);

			return project;
		}

		private static IEnumerable<string>	NonBuildProperties
		{
			get
			{
				yield return "BuildInParallel";
				yield return "Targets";
				yield return "OriginalItemSpec";
				yield return "Language";
			}
		}
		private static bool					IsBuildProperty(string name) => !Mixins.NonBuildProperties.Contains (name);
		private static void					PreprocessOptions(ITaskItem project)
		{
			Mixins.PreprocessPlatform (project);
			Mixins.PreprocessOutDir (project);
		}


		private static void					PreprocessPlatform(ITaskItem project)
		{
			// Preprocess the 'Any CPU' platform property.
			// If the item spec is a .sln use 'Any CPU' (with a space).
			// If the item spec is a .csproj use 'AnyCPU' (without a space).
			var platform = project.GetMetadata ("Platform");
			if (!string.IsNullOrEmpty(platform))
			{
				platform = platform.ToLowerInvariant ();
				if (platform == "any cpu" || platform == "anycpu")
				{
					if (project.ItemSpec.EndsWith (".sln", StringComparison.OrdinalIgnoreCase))
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
		private static void					PreprocessOutDir(ITaskItem project)
		{
			if (project.UseOutputPath () && string.IsNullOrEmpty(project.GetMetadata ("OutPutPath")))
			{
				var outDir = project.GetMetadata ("OutDir");
				if (!string.IsNullOrEmpty(outDir))
				{
					project.SetMetadata ("OutputPath", outDir);
					project.RemoveMetadata ("OutDir");
				}
			}
		}
		private static bool					UseOutputPath(this ITaskItem item)			=> item.IsCsProj () || item.GetLanguageMetaData () == Language.CSharp;
		private static bool					IsCsProj(this ITaskItem item)				=> item.ItemSpec.EndsWith (".csproj", StringComparison.OrdinalIgnoreCase);
		private static Language				GetLanguageMetaData(this ITaskItem item)	=> item.GetMetadata ("Language").ToLanguage ();
		private static Language				ToLanguage(this string value)
		{
			if (string.IsNullOrWhiteSpace (value))
			{
				return Language.None;
			}
			else if (value.StartsWith ("C#", StringComparison.OrdinalIgnoreCase) ||
				     value.StartsWith ("CSharp", StringComparison.OrdinalIgnoreCase))
			{
				return Language.CSharp;
			}
			else if (value.StartsWith ("C++", StringComparison.OrdinalIgnoreCase) ||
				     value.StartsWith ("Cpp", StringComparison.OrdinalIgnoreCase))
			{
				return Language.Cpp;
			}
			else
			{
				throw new ArgumentOutOfRangeException (nameof (value), $"Language '{value}' not defined in zou");
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
