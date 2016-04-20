using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.BuildEngine;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Epsitec.Zou
{
	public enum Language
	{
		NotSpecified,
		Cpp,
		CSharp
	}
	public enum Platform
	{
		NotSpecified,
		AnyCpu,
		Win32,
		x86,
		x64,
		Arm,
		Itanium
	}
	public enum ProjectType
	{
		Sln,
		CsProj,
		VcxProj,
	}

	public class AddBuildOptions : Task
	{
		public override bool			Execute()
		{
			this.ProjectsOutput = this.Projects.Select (project => this.AddOptions (project)).ToArray();
			return !this.Log.HasLoggedErrors;
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

		private ITaskItem				AddOptions(ITaskItem projectItem)
		{
			var project = new TaskItem (projectItem);
			this.PreprocessOptions (project);

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
		private void					PreprocessOptions(ITaskItem project)
		{
			try
			{
				var projectType = project.GetProjectType ();
				var language    = this.PreprocessLanguage (project, projectType);
				var platform    = this.PreprocessPlatform (project, projectType, language);
				language        = this.PreprocessLanguage (project, projectType, language, platform);
				this.PreprocessOutDir (project, projectType, language, platform);
			}
			catch (Exception e)
			{
				this.Log.LogErrorFromException (e);
			}
		}
		private Language				PreprocessLanguage(ITaskItem project, ProjectType projectType)
		{
			Language language;
			return ProjectToLanguage.TryGetValue (projectType, out language) ? language : project.GetLanguageMetadata ();
		}
		private Platform				PreprocessPlatform(ITaskItem project, ProjectType projectType, Language language)
		{
			var platform = project.GetPlatformMetadata ();
			if (platform == Platform.Win32)
			{
				if (language == Language.CSharp)
				{
					platform = Platform.AnyCpu;
				}
			}

			if (platform == Platform.AnyCpu)
			{
				if (projectType == ProjectType.Sln)
				{
					project.SetMetadata ("Platform", "Any CPU");
				}
				else
				{
					project.SetMetadata ("Platform", "AnyCPU");
				}
			}
			return platform;
		}
		private Language				PreprocessLanguage(ITaskItem project, ProjectType projectType, Language language, Platform platform)
		{
			if (language == Language.NotSpecified)
			{
				if (platform == Platform.AnyCpu)
				{
					return Language.CSharp;
				}
				this.Log.LogError ($"Language cannot be resolved for solution {project.ItemSpec}, please specify:\n  add Language metadata in ImportProject item (exemple: <Language>C#<Language>) \n  or specify it on the command line (exemple: /p:Language=C#)");
			}
			return language;
		}
		private void					PreprocessOutDir(ITaskItem project, ProjectType projectType, Language language, Platform platform)
		{
			if (language == Language.CSharp && string.IsNullOrEmpty(project.GetMetadata ("OutPutPath")))
			{
				var outDir = project.GetMetadata ("OutDir");
				if (!string.IsNullOrEmpty(outDir))
				{
					project.SetMetadata ("OutputPath", outDir);
					project.RemoveMetadata ("OutDir");
				}
			}
		}

		private static readonly Dictionary<ProjectType, Language> ProjectToLanguage = new Dictionary<ProjectType, Language>()
		{
			{ ProjectType.CsProj,  Language.CSharp },
			{ ProjectType.VcxProj, Language.Cpp },
		};
	}

	internal class BuildPropertyComparer : IComparer<Tuple<string, string>>
	{
		public static readonly BuildPropertyComparer Default = new BuildPropertyComparer ();

		public int							Compare(Tuple<string, string> x, Tuple<string, string> y)
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
	internal static partial class Mixins
	{

		public static bool					IsBuildProperty(string name)				=> !Mixins.NonBuildProperties.Contains (name);
		public static ProjectType			GetProjectType(this ITaskItem item)			=> item.ItemSpec.ToProjectType ();
		public static Platform				GetPlatformMetadata(this ITaskItem item)	=> item.GetMetadata ("Platform").ToPlatform ();
		public static Language				GetLanguageMetadata(this ITaskItem item)	=> item.GetMetadata ("Language").ToLanguage ();

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
		private static ProjectType			ToProjectType(this string itemSpec)
		{
			var extension = Path.GetExtension (itemSpec);
			ProjectType projectType;
			if (Enum.TryParse<ProjectType> (extension.Substring (1), true, out projectType))
			{
				return projectType;
			}
			throw new ArgumentOutOfRangeException (nameof (extension), $"Project type '{extension}' not defined in zou");
		}
		private static Platform				ToPlatform(this string value)
		{
			if (string.IsNullOrWhiteSpace (value))
			{
				return Platform.NotSpecified;
			}
			else if (0 == string.Compare(value, "Any CPU", StringComparison.OrdinalIgnoreCase))
			{
				return Platform.AnyCpu;
			}
			Platform platform;
			if (Enum.TryParse(value, true, out platform))
			{
				return platform;
			}
			throw new ArgumentOutOfRangeException (nameof (value), $"Platform '{value}' not defined in zou");
		}
		private static Language				ToLanguage(this string value)
		{
			if (string.IsNullOrWhiteSpace (value))
			{
				return Language.NotSpecified;
			}
			else if (0 == string.Compare (value, "C#", StringComparison.OrdinalIgnoreCase))
			{
				return Language.CSharp;
			}
			else if (0 == string.Compare (value, "C++", StringComparison.OrdinalIgnoreCase))
			{
				return Language.Cpp;
			}
			Language language;
			if (Enum.TryParse (value, true, out language))
			{
				return language;
			}
			throw new ArgumentOutOfRangeException (nameof (value), $"Language '{value}' not defined in zou");
		}
	}
}
