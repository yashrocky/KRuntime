﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.PackageManager;

namespace Microsoft.Framework.Packages
{
    /// <summary>
    /// Summary description for FeedOptions
    /// </summary>
    public class PackagesOptions
    {
        public PackagesOptions()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        public string SourcePackages { get; set; }

        public Reports Reports { get; set; }
    }
}
