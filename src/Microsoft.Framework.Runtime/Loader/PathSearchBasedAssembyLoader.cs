// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Reflection;

namespace Microsoft.Framework.Runtime.Loader
{
    public class PathSearchBasedAssemblyLoader : IAssemblyLoader
    {
        private static readonly string[] _extensions = new string[] { ".dll", ".exe" };

        private readonly IAssemblyLoadContextAccessor _loadContextAccessor;
        private readonly string[] _searchPaths;

        public PathSearchBasedAssemblyLoader(IApplicationEnvironment env)
        {
            _loadContextAccessor = LoadContextAccessor.Instance;
            _searchPaths = env.SearchPaths;
        }

        public Assembly Load(string name)
        {
            return Load(name, _loadContextAccessor.Default);
        }

        public Assembly Load(string name, IAssemblyLoadContext loadContext)
        {
            loadContext = _loadContextAccessor.Default;

            foreach (var path in _searchPaths)
            {
                foreach (var extension in _extensions)
                {
                    var filePath = Path.Combine(path, name + extension);

                    if (File.Exists(filePath))
                    {
                        return loadContext.LoadFile(filePath);
                    }
                }
            }

            return null;
        }
    }
}