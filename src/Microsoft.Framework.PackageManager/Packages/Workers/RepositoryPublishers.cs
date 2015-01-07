﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Framework.PackageManager.Packages.Workers
{
    /// <summary>
    /// Summary description for RepositoryPublishers
    /// </summary>
    public static class RepositoryPublishers
    {
        public static IRepositoryPublisher Create(
            string path,
            string accessKey,
            Reports reports)
        {
            return new FileSystemRepositoryPublisher(path)
            {
                Reports = reports
            };
        }
    }
}