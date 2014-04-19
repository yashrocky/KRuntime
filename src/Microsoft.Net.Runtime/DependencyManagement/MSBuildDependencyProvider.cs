using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Xml.Linq;
using NuGet;

namespace Microsoft.Net.Runtime
{
    public class MSBuildDependencyProvider : IDependencyProvider
    {
        private readonly IProjectResolver _projectResolver;
        private readonly Dictionary<string, MsBuildProject> _resolvedProjects = new Dictionary<string, MsBuildProject>(StringComparer.OrdinalIgnoreCase);

        public MSBuildDependencyProvider(IProjectResolver projectResolver)
        {
            _projectResolver = projectResolver;
        }

        public IDictionary<string, MsBuildProject> ResolvedProjects
        {
            get
            {
                return _resolvedProjects;
            }
        }

        public LibraryDescription GetDescription(string name, SemanticVersion version, FrameworkName targetFramework)
        {
            if (version != null)
            {
                return null;
            }

            string projectFilePath = null;

            foreach (var path in _projectResolver.SearchPaths)
            {
                string file = Path.Combine(path, name, name + ".csproj");

                if (File.Exists(file))
                {
                    projectFilePath = file;
                    break;
                }
            }

            if (string.IsNullOrEmpty(projectFilePath))
            {
                return null;
            }

            XDocument document = null;
            using (var stream = File.OpenRead(projectFilePath))
            {
                document = XDocument.Load(stream);
            }

            var project = new MsBuildProject
            {
                Name = GetAssemblyName(document),
                Path = projectFilePath,
                ProjectReferences = GetProjectReferences(document),
                Document = document
            };

            _resolvedProjects[name] = project;

            return new LibraryDescription
            {
                Identity = new Library { Name = project.Name, Version = new SemanticVersion("1.0.0") },
                Dependencies = GetDependencies(project, targetFramework)
            };
        }

        private IEnumerable<Library> GetDependencies(MsBuildProject project, FrameworkName targetFramework)
        {
            var dependencies = new List<Library>();

            foreach (var projectReferencePath in project.ProjectReferences)
            {
                // TODO: Detect the project's target framework

                dependencies.Add(new Library
                {
                    Name = Path.GetFileNameWithoutExtension(projectReferencePath),
                    Version = new SemanticVersion("1.0.0") // TODO: Version
                });
            }

            foreach (var packageNode in GetPackageReferences(project.Path))
            {
                var packageTargetFramework = packageNode.Attribute("targetFramework").Value;

                if (packageTargetFramework != null &&
                    !VersionUtility.IsCompatible(targetFramework, VersionUtility.ParseFrameworkName(packageTargetFramework)))
                {
                    continue;
                }

                dependencies.Add(new Library
                {
                    Name = packageNode.Attribute("id").Value,
                    Version = SemanticVersion.Parse(packageNode.Attribute("version").Value)
                });
            }

            return dependencies;
        }

        public void Initialize(IEnumerable<LibraryDescription> dependencies, FrameworkName targetFramework)
        {
            foreach (var dependency in dependencies)
            {
                dependency.Path = _resolvedProjects[dependency.Identity.Name].Path;
                dependency.Type = "Project";
            }
        }

        private static string GetAssemblyName(XDocument document)
        {
            return document
                .Elements(ns("Project"))
                .Elements(ns("PropertyGroup"))
                .Elements(ns("AssemblyName"))
                .Single()
                .Value;
        }

        private static IEnumerable<string> GetProjectReferences(XDocument document)
        {
            return document
                .Elements(ns("Project"))
                .Elements(ns("ItemGroup"))
                .Elements(ns("ProjectReference"))
                .Attributes("Include")
                .Select(c => c.Value);
        }

        private static IEnumerable<XElement> GetPackageReferences(string projectFile)
        {
            var packagesConfig = Path.Combine(Path.GetDirectoryName(projectFile), "packages.config");

            if (!File.Exists(packagesConfig))
            {
                return Enumerable.Empty<XElement>();
            }

            XDocument document = null;
            using (var stream = File.OpenRead(packagesConfig))
            {
                document = XDocument.Load(stream);
            }

            return document.Root.Elements();
        }

        private static XName ns(string name)
        {
            return XName.Get(name, "http://schemas.microsoft.com/developer/msbuild/2003");
        }
    }

    public class MsBuildProject
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public IEnumerable<string> ProjectReferences { get; set; }
        public XDocument Document { get; set; }
    }
}