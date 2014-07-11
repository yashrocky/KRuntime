// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace NuGet
{
    internal static class EnumerableExtensions
    {
        public static bool IsEmpty<T>(this IEnumerable<T> sequence)
        {
            return sequence == null || !sequence.Any();
        }
    }
}
