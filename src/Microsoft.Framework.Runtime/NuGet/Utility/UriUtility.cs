// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;

namespace NuGet
{
    public static class UriUtility
    {
#if NET45
        public static Uri CreatePartUri(string path)
        {
            // Only the segments between the path separators should be escaped
            var segments = path.Split(new[] { '/', Path.DirectorySeparatorChar }, StringSplitOptions.None)
                               .Select(Uri.EscapeDataString);
            var escapedPath = String.Join("/", segments);
            return System.IO.Packaging.PackUriHelper.CreatePartUri(new Uri(escapedPath, UriKind.Relative));
        }
#endif

        /// <summary>
        /// Converts a uri to a path. Only used for local paths.
        /// </summary>
        public static string GetPath(Uri uri)
        {
            string path = uri.OriginalString;
            if (path.StartsWith("/", StringComparison.Ordinal))
            {
                path = path.Substring(1);
            }

            // Bug 483: We need the unescaped uri string to ensure that all characters are valid for a path.
            // Change the direction of the slashes to match the filesystem.
            return Uri.UnescapeDataString(path.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
