// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Framework.PackageManager.DependencyAnalyzer
{
    internal class DependencyListOptions
    {
        public DependencyListOptions(string path, bool local, string coreclrRoot, Reports reports)
        {
            Runtime.Project proj;
            Valid = Runtime.Project.TryGetProject(path, out proj);

            Path = path;
            Local = local;
            CoreClrRoot = coreclrRoot;
            Project = proj;
            Reports = reports;
        }

        public string Path { get; private set; }

        public Runtime.Project Project { get; private set; }

        public string CoreClrRoot { get; private set; }

        public bool Local { get; private set; }

        public bool Valid { get; private set; }

        public Reports Reports { get; private set; }
    }
}