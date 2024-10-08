// Copyright © 2013-2021, EPSITEC SA, CH-1400 Yverdon-les-Bains, Switzerland
// Author: Roger VUISTINER, Maintainer: Roger VUISTINER

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Bcx.Linq;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Zou.Tasks
{
    /// <summary>
    /// set MSBuildEmitSolution=1<br/>
    /// msbuild myapp.sln -> myapp.sln.metaproj
    /// </summary>
    public enum ProjectType
    {
        CSharp,
        Cloud,
        Cpp,
        DataBase,
        Dependency,
        Docker,
        FSharp,
        Go,
        Goblin,
        Js,
        MetaProj,
        MsBuild,
        NodeJs,
        PowerShell,
        Shared,
        Solution,
        Sql,
        VisualBasic,
        Zou,
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
                var propertyKeyValues = names
                    .Where(name => Mixins.IsBuildProperty(name))
                    .Select(name => (Key: name, Value: project.GetMetadata(name)))
                    .Where(kv => !string.IsNullOrWhiteSpace(kv.Value) && kv.Value != Mixins.UndefinedValue)
                    .OrderBy(kv => kv, BuildPropertyComparer.Default)
                    .Select(kv => $"{kv.Key}={QuotePropertyValue(kv.Value)}")
                    .ToArray();

                // Create the MSBuild task 'Properties' attribute
                var properties = string.Join(";", propertyKeyValues);
                project.SetMetadata("Properties", properties);

                // Create the 'msbuild' command property options (-p:prop1=value1 -p:prop2=value2)
                // There is an issue when property-value pairs are concatenated with ';' (-p:prop1=value1;prop2=value2)
                // If the 'TargetFramework' property-value is not the first in the list, the nuget restore fails.
                //      -p:TargetFramework=net6;Configuration=Release  // OK
                //      -p:Configuration=Release;TargetFramework=net6  // NOT OK
                // Split the options to avoid this issue (-p:prop1=value1 -p:prop2=value2)
                var propertyOptions = string.Join(" ", propertyKeyValues.Select(kv => $"-p:{kv}"));
                project.SetMetadata("PropertyOptions", propertyOptions);

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
                    yield return QuoteValue(targetsValue);
                }
                else
                {
                    yield return cleanTarget;
                    var otherTargets = targets.Except(EnumerableEx.Return(cleanTarget));
                    if (otherTargets.Any())
                    {
                        yield return QuoteValue(string.Join(";", otherTargets));
                    }
                }
            }
            static string QuotePropertyValue(string value)
            {
                // https://github.com/dotnet/sdk/issues/8792#issuecomment-393756980
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    if (value.Contains(';'))
                    {
                        value = $"\\\"{value}\\\"";   // -p:NoWarn=\"1591;1573;3001;3002\"
                    }
                    else if (value.Contains(' '))
                    {
                        value = $"\"{value}\"";       // -p:OutDir="my project directory"
                    }
                }
                else
                {
                    if (value.Contains(';'))
                    {
                        value = $"'\"{value}\"'";   // -p:NoWarn='"1591;1573;3001;3002"'
                    }
                    else if (value.Contains(' '))
                    {
                        value = $"'{value}'";       // -p:OutDir='my project directory'
                    }
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
        public const string UndefinedValue = "*Undefined*";

        public static bool          IsBuildProperty(string name) => !NonBuildProperties.Contains(name);
        public static Platform      GetPlatform(this ITaskItem item)
        {
            var metadata = item.GetMetadata("Platform");
            if (string.IsNullOrWhiteSpace(metadata) || metadata == UndefinedValue)
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
            { ".csproj",      ProjectType.CSharp      },
            { ".ccproj",      ProjectType.Cloud       },
            { ".vcxproj",     ProjectType.Cpp         },
            { ".dbproj",      ProjectType.DataBase    },
            { ".modelproj",   ProjectType.Dependency  },
            { ".dcproj",      ProjectType.Docker      },
            { ".fsproj",      ProjectType.FSharp      },
            { ".goproj",      ProjectType.Go          },
            { ".goblinproj",  ProjectType.Goblin      },
            { ".jsproj",      ProjectType.Js          },
            { ".metaproj",    ProjectType.MetaProj    },
            { ".msbuildproj", ProjectType.MsBuild     },
            { ".njsproj",     ProjectType.NodeJs      },
            { ".pssproj",     ProjectType.PowerShell  },
            { ".shproj",      ProjectType.Shared      },
            { ".sln",         ProjectType.Solution    },
            { ".sqlproj",     ProjectType.Sql         },
            { ".vbproj",      ProjectType.VisualBasic },
            { ".zouproj",     ProjectType.Zou         },
        };
    }
}
