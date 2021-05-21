// Copyright © 2013-2021, EPSITEC SA, CH-1400 Yverdon-les-Bains, Switzerland
// Author: Roger VUISTINER, Maintainer: Roger VUISTINER

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Zou.Tasks
{
    public enum ProjectType
    {
        Solution,
        /// <summary>
        /// set MSBuildEmitSolution=1
        /// msbuild myapp.sln -> myapp.sln.metaproj
        /// </summary>
        MetaProj,
        CSharp,
        Cpp,
        PowerShell,
        FSharp,
        VisualBasic,
        Dependency,
        Docker,
        Shared,
        Cloud,
        NodeJs,
        Js,
        MsBuild,
        Sql,
        DataBase
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
        [Required] public ITaskItem[]   Projects       { get; set; }
        [Output]   public ITaskItem[]   ProjectsOutput { get; private set; }

        public override bool            Execute()
        {
            //Debugger.Launch();

            this.ProjectsOutput = this.Projects.SelectMany(project => this.AddOptions(project)).ToArray();
            return !this.Log.HasLoggedErrors;
        }

        private IEnumerable<ITaskItem>  AddOptions(ITaskItem projectItem)
        {
            var template = new TaskItem(projectItem);
            this.ProcessOptions(template);

            // create a specific project for clean target
            var targetsValue  = template.GetMetadata("Targets");
            var targetsGroups = IsolateCleanTarget(targetsValue);

            foreach (var targets in targetsGroups)
            {
                var project = new TaskItem(template);
                // Add Targets metadata
                project.SetMetadata("Targets", targets);

                var names = project.CustomMetadataNames();
                // Add Properties metadata
                var properties = names
                    .Where(name => Mixins.IsBuildProperty(name))
                    .Select(name => (Key: name, Value: project.GetMetadata(name)))
                    .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
                    .OrderBy(kv => kv, BuildPropertyComparer.Default)
                    .Select(kv => $"{kv.Key}={QuotePropertyValue(kv.Value)}")
                    .ToArray();

                var propertiesValue = string.Join(";", properties);
                if (propertiesValue.EndsWith("\\"))
                {
                    propertiesValue += ";_=_";
                }
                propertiesValue = QuoteValue(propertiesValue);
                project.SetMetadata("Properties", propertiesValue);


                yield return project;
            }

            static IEnumerable<string> IsolateCleanTarget(string targetsValue)
            {
                var targets = targetsValue.Split(';');
                var cleanTarget = targets
                    .Where(target => target.Equals("Clean", StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();

                if (cleanTarget == null)
                {
                    yield return targetsValue;
                }
                else
                {
                    yield return cleanTarget;
                    // group other targets
                    yield return QuoteValue(string.Join(";", targets.Except(EnumerableEx.Return(cleanTarget))));
                }
            }
            static string QuotePropertyValue(string value)
            {
                // https://github.com/dotnet/sdk/issues/8792#issuecomment-393756980
                if (value.Contains(';'))
                {
                    value = Environment.OSVersion.Platform switch
                    {
                        PlatformID.Win32NT => $"\\\"{value}\\\"",
                        _ => $"'\"{value}\"'"
                    };
                }
                return value;
            }
            static string QuoteValue(string value)
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    if (value.Contains(' '))
                    {
                        value = $"\"{value}\"";
                    }
                }
                else
                {
                    if (value.Contains(';') || value.Contains(' '))
                    {
                        value = $"'{value}'";
                    }
                }
                return value;
            }
        }
        private void                    ProcessOptions(ITaskItem project)
        {
            try
            {
                var projectType = project.GetProjectType();
                var platform = this.ProcessPlatform(project, projectType);
                this.ProcessOutDir(project, projectType, platform);
            }
            catch (Exception e)
            {
                this.Log.LogErrorFromException(e);
            }
        }
        private Platform                ProcessPlatform(ITaskItem project, ProjectType projectType)
        {
            var platform = project.GetPlatform();
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
                    project.SetMetadata("Platform", "Any CPU");
                }
                else
                {
                    project.SetMetadata("Platform", "AnyCPU");
                }
            }
            return platform;
        }
        private void                    ProcessOutDir(ITaskItem project, ProjectType projectType, Platform platform)
        {
            if (projectType == ProjectType.CSharp && string.IsNullOrEmpty(project.GetMetadata("OutPutPath")))
            {
                var outDir = project.GetMetadata("OutDir");
                if (!string.IsNullOrEmpty(outDir))
                {
                    if (0 != string.Compare("Publish", project.GetMetadata("_Target"), StringComparison.OrdinalIgnoreCase))
                    {
                        project.SetMetadata("OutputPath", outDir);
                    }
                    project.RemoveMetadata("OutDir");
                }
            }
        }
    }

    internal class BuildPropertyComparer : IComparer<(string Key, string Value)>
    {
        public static readonly BuildPropertyComparer Default = new BuildPropertyComparer();

        public int Compare((string Key, string Value) x, (string Key, string Value) y)
        {
            var xEndsWithBackslash = x.Value.EndsWith("\\");
            var yEndsWithBackslash = y.Value.EndsWith("\\");
            if (xEndsWithBackslash)
            {
                return yEndsWithBackslash ? string.Compare(x.Key, y.Key) : -1;
            }
            else if (yEndsWithBackslash)
            {
                return xEndsWithBackslash ? string.Compare(x.Key, y.Key) : 1;
            }
            else
            {
                return string.Compare(x.Key, y.Key);
            }
        }
    }
    internal static partial class Mixins
    {
        public static bool          IsBuildProperty(string name) => !NonBuildProperties.Contains(name);
        public static Platform      GetPlatform(this ITaskItem item)
        {
            var metadata = item.GetMetadata("Platform");
            if (string.IsNullOrWhiteSpace(metadata))
            {
                return Platform.NotSpecified;
            }
            else if (0 == string.Compare(metadata, "Any CPU", StringComparison.OrdinalIgnoreCase))
            {
                return Platform.AnyCpu;
            }
            if (Enum.TryParse(metadata, true, out Platform platform))
            {
                return platform;
            }
            throw new ArgumentOutOfRangeException(nameof(metadata), $"Platform '{metadata}' not defined in zou");
        }
        public static ProjectType   GetProjectType(this ITaskItem item)
        {
            var extension = Path.GetExtension(item.ItemSpec);
            if (ProjectExtensionToType.TryGetValue(extension, out var projectType))
            {
                return projectType;
            }
            throw new ArgumentOutOfRangeException(nameof(extension), $"Project type '{extension}' not defined in zou");
        }

        private static IEnumerable<string> NonBuildProperties
        {
            get
            {
                yield return "BuildInParallel";
                yield return "Language";
                yield return "Link";
                yield return "OriginalItemSpec";
                yield return "Targets";
                yield return "Verbosity";
            }
        }

        private static readonly Dictionary<string, ProjectType> ProjectExtensionToType = new Dictionary<string, ProjectType>()
        {
            { ".sln",         ProjectType.Solution },
            { ".metaproj",    ProjectType.MetaProj },
            { ".csproj",      ProjectType.CSharp },
            { ".vcxproj",     ProjectType.Cpp },
            { ".pssproj",     ProjectType.PowerShell },
            { ".fsproj",      ProjectType.FSharp },
            { ".vbproj",      ProjectType.VisualBasic },
            { ".modelproj",   ProjectType.Dependency },
            { ".dcproj",      ProjectType.Docker },
            { ".shproj",      ProjectType.Shared },
            { ".ccproj",      ProjectType.Cloud },
            { ".njsproj",     ProjectType.NodeJs },
            { ".jsproj",      ProjectType.Js },
            { ".msbuildproj", ProjectType.MsBuild },
            { ".sqlproj",     ProjectType.Sql },
            { ".dbproj",      ProjectType.DataBase },
        };
    }
}
