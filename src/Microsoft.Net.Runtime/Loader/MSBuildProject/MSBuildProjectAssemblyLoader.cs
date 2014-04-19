using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Xml.Linq;
using Microsoft.Net.Runtime.FileSystem;

namespace Microsoft.Net.Runtime.Loader.MSBuildProject
{
    public class MSBuildEngine
    {
        private readonly static string[] _msBuildPaths = new string[] {
            Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%"), @"MSBuild\12.0\Bin\MSBuild.exe"),
            Path.Combine(Environment.ExpandEnvironmentVariables("%Windows%"), @"Microsoft.NET\Framework\v4.0.30319\MSBuild.exe")
        };

        private readonly IFileWatcher _watcher;

        public MSBuildEngine(IFileWatcher watcher)
        {
            _watcher = watcher;
        }

        public string BuildProject(string name, string projectFile)
        {
            string msbuildPath = null;

            foreach (var exePath in _msBuildPaths)
            {
                if (File.Exists(exePath))
                {
                    msbuildPath = exePath;
                    break;
                }
            }

            if (string.IsNullOrEmpty(msbuildPath))
            {
                return null;
            }

            WatchProject(projectFile);

            string projectDir = Path.GetDirectoryName(projectFile);
            var executable = new Executable(msbuildPath, projectDir);

            string outputFile = null;
            var process = executable.Execute(line =>
            {
                // Look for {project} -> {outputPath}
                int index = line.IndexOf('-');

                System.Diagnostics.Trace.TraceInformation("[{0}]:{1}", GetType().Name, line);

                if (index != -1 && index + 1 < line.Length && line[index + 1] == '>')
                {
                    string projectName = line.Substring(0, index).Trim();
                    if (projectName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        outputFile = line.Substring(index + 2).Trim();
                    }
                }

                return true;
            },
            _ => true,
            "\"" + projectFile + "\"" + " /m /p:Configuration=Debug;Platform=AnyCPU");

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                // REVIEW: Should this throw?
                return null;
            }

            return outputFile;
        }

        private void WatchProject(string projectFile)
        {
            // We're already watching this file
            if (!_watcher.WatchFile(projectFile))
            {
                return;
            }

            string projectDir = Path.GetDirectoryName(projectFile);

            _watcher.WatchFile(Path.Combine(projectDir, "packages.config"));

            XDocument document = null;
            using (var stream = File.OpenRead(projectFile))
            {
                document = XDocument.Load(stream);
            }

            foreach (var contentItem in GetSourceFilenames(document))
            {
                var path = Path.Combine(projectDir, contentItem);
                _watcher.WatchFile(Path.GetFullPath(path));
            }

            // Watch project references
            foreach (var projectReferencePath in GetProjectReferences(document))
            {
                string path = Path.GetFullPath(Path.Combine(projectDir, projectReferencePath));

                WatchProject(path);
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

        private static IEnumerable<string> GetSourceFilenames(XDocument document)
        {
            return document
                .Elements(ns("Project"))
                .Elements(ns("ItemGroup"))
                .Elements(ns("Compile"))
                .Attributes("Include")
                .Select(c => c.Value);
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

        private static XName ns(string name)
        {
            return XName.Get(name, "http://schemas.microsoft.com/developer/msbuild/2003");
        }
    }

    public class MSBuildLibraryExportProvider : ILibraryExportProvider
    {
        private readonly MSBuildDependencyProvider _dependencyProvider;

        public MSBuildLibraryExportProvider(MSBuildDependencyProvider dependencyProvider)
        {
            _dependencyProvider = dependencyProvider;
        }

        public ILibraryExport GetLibraryExport(string name, FrameworkName targetFramework)
        {
            MsBuildProject project;
            if (!_dependencyProvider.ResolvedProjects.TryGetValue(name, out project))
            {
                return null;
            }

            var engine = new MSBuildEngine(NoopWatcher.Instance);

            var path = engine.BuildProject(name, project.Path);

            if (path == null)
            {
                return null;
            }

            Trace.TraceInformation("[{0}]: Resolved export path {1}", GetType().Name, path);

            return new LibraryExport(path);
        }
    }

    public class MSBuildProjectAssemblyLoader : IAssemblyLoader
    {
        private readonly MSBuildEngine _buildEngine;

        private readonly static string[] _msBuildPaths = new string[] {
            Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramFiles(x86)%"), @"MSBuild\12.0\Bin\MSBuild.exe"),
            Path.Combine(Environment.ExpandEnvironmentVariables("%Windows%"), @"Microsoft.NET\Framework\v4.0.30319\MSBuild.exe")
        };

        private readonly IAssemblyLoaderEngine _loaderEngine;
        private readonly MSBuildDependencyProvider _dependencyProvider;

        public MSBuildProjectAssemblyLoader(IAssemblyLoaderEngine loaderEngine,
                                            MSBuildEngine buildEngine,
                                            MSBuildDependencyProvider dependencyProvider)
        {
            _dependencyProvider = dependencyProvider;
            _buildEngine = buildEngine;
            _loaderEngine = loaderEngine;
        }

        public AssemblyLoadResult Load(LoadContext loadContext)
        {
            string name = loadContext.AssemblyName;

            MsBuildProject project;
            if (!_dependencyProvider.ResolvedProjects.TryGetValue(name, out project))
            {
                return null;
            }

            var path = _buildEngine.BuildProject(name, project.Path);

            if (path == null)
            {
                return null;
            }

            // HACK so we don't need to worry about locked files
            var assemblyBytes = File.ReadAllBytes(path);
            var pdbPath = Path.ChangeExtension(path, ".pdb");
            byte[] pdbBytes = null;

            if (File.Exists(pdbPath))
            {
                pdbBytes = File.ReadAllBytes(pdbPath);
            }

            return new AssemblyLoadResult(_loaderEngine.LoadBytes(assemblyBytes, pdbBytes));
        }
    }
}
