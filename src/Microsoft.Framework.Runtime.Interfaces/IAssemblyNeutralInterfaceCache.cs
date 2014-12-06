// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;

namespace Microsoft.Framework.Runtime
{
    [AssemblyNeutral]
    public interface IAssemblyNeutralInterfaceCache
    {
        Assembly GetAssembly(string name);

        bool IsLoaded(string name);

        void AddAssembly(string name, Assembly assembly);
    }
}