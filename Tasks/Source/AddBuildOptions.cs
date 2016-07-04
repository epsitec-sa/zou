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
	public enum ProjectType
	{
		Solution,
		CSharp,
		Cpp,
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

	public class AddBuildOptions : Task
	{
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
		public override bool			Execute()
		{
			this.ProjectsOutput = this.Projects.Select (project => this.AddOptions (project)).ToArray ();
			return !this.Log.HasLoggedErrors;
		}

		private ITaskItem				AddOptions(ITaskItem projectItem)
		{
			var project = new TaskItem (projectItem);
			this.ProcessOptions (project);

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
		private void					ProcessOptions(ITaskItem project)
		{
			try
			{
				var projectType = project.GetProjectType ();
				var platform    = this.ProcessPlatform (project, projectType);
				this.ProcessOutDir (project, projectType, platform);
			}
			catch (Exception e)
			{
				this.Log.LogErrorFromException (e);
			}
		}
		private Platform				ProcessPlatform(ITaskItem project, ProjectType projectType)
		{
			var platform = project.GetPlatform ();
			if (platform == Platform.Win32)
			{
				if (projectType == ProjectType.CSharp)
				{
					platform = Platform.AnyCpu;
				}
			}

			if (platform == Platform.AnyCpu)
			{
				if (projectType == ProjectType.Solution)
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
		private void					ProcessOutDir(ITaskItem project, ProjectType projectType, Platform platform)
		{
			if (projectType == ProjectType.CSharp && string.IsNullOrEmpty(project.GetMetadata ("OutPutPath")))
			{
				var outDir = project.GetMetadata ("OutDir");
				if (!string.IsNullOrEmpty(outDir))
				{
					project.SetMetadata ("OutputPath", outDir);
					project.RemoveMetadata ("OutDir");
				}
			}
		}

	}

	internal class BuildPropertyComparer : IComparer<Tuple<string, string>>
	{
		public static readonly BuildPropertyComparer	Default = new BuildPropertyComparer ();
		public int										Compare(Tuple<string, string> x, Tuple<string, string> y)
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
		public static bool					IsBuildProperty(string name) => !Mixins.NonBuildProperties.Contains (name);
		public static Platform				GetPlatform(this ITaskItem item)
		{
			var metadata = item.GetMetadata ("Platform");
			if (string.IsNullOrWhiteSpace (metadata))
			{
				return Platform.NotSpecified;
			}
			else if (0 == string.Compare(metadata, "Any CPU", StringComparison.OrdinalIgnoreCase))
			{
				return Platform.AnyCpu;
			}
			Platform platform;
			if (Enum.TryParse(metadata, true, out platform))
			{
				return platform;
			}
			throw new ArgumentOutOfRangeException (nameof (metadata), $"Platform '{metadata}' not defined in zou");
		}
		public static ProjectType			GetProjectType(this ITaskItem item)
		{
			var extension = Path.GetExtension (item.ItemSpec);
			ProjectType projectType;
			if (ProjectExtensionToType.TryGetValue (extension, out projectType))
			{
				return projectType;
			}
			throw new ArgumentOutOfRangeException (nameof (extension), $"Project type '{extension}' not defined in zou");
		}

		private static IEnumerable<string>	NonBuildProperties
		{
			get
			{
				yield return "BuildInParallel";
				yield return "Targets";
				yield return "OriginalItemSpec";
				yield return "Language";
				yield return "Link";
			}
		}

		private static readonly Dictionary<string, ProjectType> ProjectExtensionToType = new Dictionary<string, ProjectType>()
		{
			{ ".sln",     ProjectType.Solution },
			{ ".csproj",  ProjectType.CSharp },
			{ ".vcxproj", ProjectType.Cpp },
		};
	}
}
