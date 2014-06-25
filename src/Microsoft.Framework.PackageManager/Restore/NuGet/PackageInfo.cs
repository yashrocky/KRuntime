// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Runtime;
using NuGet;

namespace Microsoft.Framework.PackageManager
{
    public class PackageInfo
    {
        public string Id { get; set; }
        public SemanticVersion2 Version { get; set; }
        public string ContentUri { get; set; }
    }
}