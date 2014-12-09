// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Framework.Runtime
{
    [AssemblyNeutral]
    public interface IProjectReferenceProvider
    {
        IMetadataProjectReference GetProjectReference(
            object project,
            ILibraryKey target,
            Func<ILibraryExport> referenceResolver,
            IList<IMetadataReference> outgoingReferences,
            Func<IAssemblyLoadContext> assemblyLoadContextResolver = null);
    }
}