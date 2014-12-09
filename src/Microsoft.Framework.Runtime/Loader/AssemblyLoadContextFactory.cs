using System;
using System.Reflection;
using Microsoft.Framework.Runtime.Common.DependencyInjection;

namespace Microsoft.Framework.Runtime.Loader
{
    public class AssemblyLoadContextFactory : IAssemblyLoadContextFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IAssemblyNeutralInterfaceCache _assemblyNeutralInterfaceCache;

        public AssemblyLoadContextFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _assemblyNeutralInterfaceCache = serviceProvider.GetService(typeof(IAssemblyNeutralInterfaceCache)) as IAssemblyNeutralInterfaceCache;
        }

        public IAssemblyLoadContext Create()
        {
            var projectAssemblyLoader = (ProjectAssemblyLoader)ActivatorUtilities.CreateInstance(_serviceProvider, typeof(ProjectAssemblyLoader));
            var nugetAsseblyLoader = (NuGetAssemblyLoader)ActivatorUtilities.CreateInstance(_serviceProvider, typeof(NuGetAssemblyLoader));
            var pathBaseAssemblyLoader = (PathSearchBasedAssemblyLoader)ActivatorUtilities.CreateInstance(_serviceProvider, typeof(PathSearchBasedAssemblyLoader));

            return new LibraryAssemblyLoadContext(projectAssemblyLoader, nugetAsseblyLoader, pathBaseAssemblyLoader, _assemblyNeutralInterfaceCache);
        }

        private class LibraryAssemblyLoadContext : LoadContext
        {
            private readonly ProjectAssemblyLoader _projectAssemblyLoader;
            private readonly NuGetAssemblyLoader _nugetAssemblyLoader;
            private readonly PathSearchBasedAssemblyLoader _pathBasedAssemblyLoader;

            public LibraryAssemblyLoadContext(ProjectAssemblyLoader projectAssemblyLoader,
                                              NuGetAssemblyLoader nugetAssemblyLoader,
                                              PathSearchBasedAssemblyLoader pathBasedAssemblyLoader,
                                              IAssemblyNeutralInterfaceCache assemblyNeutralInterfaceCache)
                : base(assemblyNeutralInterfaceCache)
            {
                _projectAssemblyLoader = projectAssemblyLoader;
                _nugetAssemblyLoader = nugetAssemblyLoader;
                _pathBasedAssemblyLoader = pathBasedAssemblyLoader;
            }

            public override Assembly LoadAssembly(string name)
            {
                return _pathBasedAssemblyLoader.Load(name, this) ??
                       _projectAssemblyLoader.Load(name, this) ??
                       _nugetAssemblyLoader.Load(name, this);
            }
        }
    }
}