// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace NuGet
{
    internal static class FileSystemExtensions
    {
        internal static IEnumerable<string> GetFiles(this IFileSystem fileSystem, string path, string filter)
        {
            return fileSystem.GetFiles(path, filter, recursive: false);
        }
    }
}