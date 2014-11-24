// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;
using System.Collections.Generic;

namespace Microsoft.Framework.Runtime
{
    [AssemblyNeutral]
    public interface IFileWatcher : IFileMonitor, IDisposable
    {
        void WatchFile(string path);

        void WatchFilePatterns(string basePath, IEnumerable<string> patterns, IEnumerable<string> excludePatterns);

        void WatchProject(string path);
    }
}
