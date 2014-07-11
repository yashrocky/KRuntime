// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace NuGet
{
    internal static class NuGetConstants
    {
        /// <summary>
        /// Represents the ".nupkg" extension.
        /// </summary>
        public static readonly string PackageExtension = ".nupkg";
        
        /// <summary>
        /// Represents the ".nuspec" extension.
        /// </summary>
        public static readonly string ManifestExtension = ".nuspec";

        /// <summary>
        /// Represents the content directory in the package.
        /// </summary>
        public static readonly string ContentDirectory = "content";

        /// <summary>
        /// Represents the lib directory in the package.
        /// </summary>
        public static readonly string LibDirectory = "lib";

        /// <summary>
        /// Represents the tools directory in the package.
        /// </summary>
        public static readonly string ToolsDirectory = "tools";

        /// <summary>
        /// Represents the build directory in the package.
        /// </summary>
        public static readonly string BuildDirectory = "build";
        
        public static readonly string BinDirectory = "bin";
        
        public const string PackageRelationshipNamespace = "http://schemas.microsoft.com/packaging/2010/07/";

        // Starting from nuget 2.0, we use a file with the special name '_._' to represent an empty folder.
        public const string PackageEmptyFileName = "_._";

        public static readonly string ManifestRelationType = "manifest";
    }
}