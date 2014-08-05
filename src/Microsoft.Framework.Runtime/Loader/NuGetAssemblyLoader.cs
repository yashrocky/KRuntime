// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System.Reflection;

namespace Microsoft.Framework.Runtime.Loader
{
    public class NuGetAssemblyLoader : IAssemblyLoader
    {
        private readonly NuGetDependencyResolver _dependencyResolver;

        public NuGetAssemblyLoader(NuGetDependencyResolver dependencyResolver)
        {
            _dependencyResolver = dependencyResolver;
        }

        public Assembly Load(IAssemblyLoadContext loadContext, string name)
        {
            string path;
            if (_dependencyResolver.PackageAssemblyPaths.TryGetValue(name, out path))
            {
                return loadContext.LoadFile(path);
            }

            return null;
        }
    }
}
