// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using Microsoft.Framework.Runtime;
using NuGet;

namespace Microsoft.Framework.PackageManager.DependencyAnalyzer
{
    internal class DependencyFinder
    {
        private readonly static FrameworkName AspNet50 = VersionUtility.ParseFrameworkName("aspnet50");
        private readonly static FrameworkName AspNetCore50 = VersionUtility.ParseFrameworkName("aspnetcore50");

        private const string LibraryTypeProject = "Project";
        private const string Configuration = "Debug";

        private readonly FrameworkName _framework;
        private readonly DependencyListOptions _options;
        private readonly ApplicationHostContext _hostContext;
        private readonly Func<string, string> _assemblyFilePathResolver;
        private readonly List<string> _unsolved;

        public DependencyFinder(DependencyListOptions options, FrameworkName framework)
        {
            _options = options;
            _framework = framework;
            _hostContext = CreateApplicationHostContext();
            _assemblyFilePathResolver = GetAssemblyFilePathResolver();

            _unsolved = new List<string>();
        }

        public IEnumerable<string> Find(bool local, out IEnumerable<string> unresolved)
        {
            var results = new HashSet<string>();
            var missing = new HashSet<string>();
            var nonProjectLibs = _hostContext.LibraryManager.GetLibraries()
                .Where(library => !string.Equals(LibraryTypeProject, library.Type, StringComparison.OrdinalIgnoreCase));
            var libLookup = _hostContext.DependencyWalker.Libraries
                .ToLookup(keySelector: desc => desc.Identity);

            if (local)
            {
                foreach (var libraryInformation in nonProjectLibs)
                {
                    foreach (var loadableAssembly in libraryInformation.LoadableAssemblies)
                    {
                        IEnumerable<string> dependencies;
                        dependencies = ResolveLocalDependency(loadableAssembly.Name, assemblyName => missing.Add(assemblyName));

                        results.AddRange(dependencies);
                    }
                }
            }
            else
            {
                foreach (var libraryInformation in nonProjectLibs)
                {
                    results.AddRange(ResolveDependency(libraryInformation.Name, libLookup, assemblyName => missing.Add(assemblyName)));
                }
            }

            unresolved = missing.ToList();

            return results;
        }

        private ISet<string> ResolveDependency(string rootReference, ILookup<Library, LibraryDescription> libLookup, Action<string> onUnresovled)
        {
            var results = new HashSet<string>();
            var rootLibrary = _hostContext.DependencyWalker.Libraries.FirstOrDefault(l => l.Identity.Name == rootReference)?.Identity;
            if (rootLibrary == null)
            {
                onUnresovled(rootReference);
            }

            var stack = new Stack<Library>();
            stack.Push(rootLibrary);

            while (stack.Count > 0)
            {
                Library current = stack.Pop();

                if (!results.Add(current.Name))
                {
                    continue;
                }

                var description = libLookup[current].FirstOrDefault();
                if (description != null)
                {
                    foreach (var each in description.Dependencies.Select(dep => dep.Library))
                    {
                        stack.Push(each);
                    }
                }
                else
                {
                    onUnresovled(current.Name);
                }
            }

            return results;
        }

        private ISet<string> ResolveLocalDependency(string rootReference, Action<string> onUnresvoled)
        {
            var result = new HashSet<string>();
            var stack = new Stack<string>();
            var gacResolver = new GacDependencyResolver();

            stack.Push(rootReference);

            while (stack.Count > 0)
            {
                string assemblyName = stack.Pop();
                string assemblyFilePath = _assemblyFilePathResolver(assemblyName);

                if (assemblyFilePath == null)
                {
                    // If the assembly file doesn't exist, try to find it in the GAC.
                    var desc = gacResolver.GetDescription(
                        new Library { Name = assemblyName, IsGacOrFrameworkReference = true },
                        _framework);

                    if (desc == null)
                    {
                        onUnresvoled(assemblyName);
                    }

                    continue;
                }

                if (!result.Add(assemblyFilePath))
                {
                    continue;
                }

                var assemblyInfo = new AssemblyInformation(assemblyFilePath, null);
                foreach (var reference in assemblyInfo.GetDependencies())
                {
                    stack.Push(reference);
                }
            }

            return result;
        }

        private ApplicationHostContext CreateApplicationHostContext()
        {
            var accessor = new CacheContextAccessor();
            var cache = new Cache(accessor);

            var hostContext = new ApplicationHostContext(
                serviceProvider: null,
                projectDirectory: _options.Project.ProjectDirectory,
                packagesDirectory: null,
                configuration: Configuration,
                targetFramework: _framework,
                cache: cache,
                cacheContextAccessor: accessor,
                namedCacheDependencyProvider: null);

            hostContext.DependencyWalker.Walk(hostContext.Project.Name, hostContext.Project.Version, _framework);

            return hostContext;
        }

        private Func<string, string> GetAssemblyFilePathResolver()
        {
            if (_framework == AspNet50)
            {
                return assemblyName =>
                {
                    // Look into the NuGets
                    PackageAssembly assembly;
                    if (_hostContext.NuGetDependencyProvider.PackageAssemblyLookup.TryGetValue(assemblyName, out assembly))
                    {
                        return assembly.Path;
                    }

                    return null;
                };
            }
            else if (_framework == AspNetCore50)
            {
                return assemblyName =>
                {
                    // Look into the CoreCLR folder first. 
                    if (_options.CoreClrRoot != null)
                    {
                        var coreclrAssemblyFilePath = Path.Combine(_options.CoreClrRoot, assemblyName + ".dll");
                        if (File.Exists(coreclrAssemblyFilePath))
                        {
                            return coreclrAssemblyFilePath;
                        }
                    }

                    // Look into the NuGets then.
                    PackageAssembly assembly;
                    if (_hostContext.NuGetDependencyProvider.PackageAssemblyLookup.TryGetValue(assemblyName, out assembly))
                    {
                        return assembly.Path;
                    }

                    return null;
                };
            }

            throw new InvalidOperationException("Unknown framework \"" + _framework.ToString() + "\".");
        }
    }
}