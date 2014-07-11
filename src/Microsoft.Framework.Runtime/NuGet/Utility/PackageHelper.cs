// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace NuGet
{
    public static class PackageHelper
    {
        public static bool IsManifest(string path)
        {
            return Path.GetExtension(path).Equals(NuGetConstants.ManifestExtension, StringComparison.OrdinalIgnoreCase);
        }
    }
}
