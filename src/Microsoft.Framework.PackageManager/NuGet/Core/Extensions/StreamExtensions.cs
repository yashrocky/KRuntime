// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace NuGet
{
    internal static class StreamExtensions
    {
        /// <summary>
        /// Turns an existing stream into one that a stream factory that can be reopened.
        /// </summary>        
        public static Func<Stream> ToStreamFactory(this Stream stream)
        {
            byte[] buffer;

            using (var ms = new MemoryStream())
            {
                try
                {
                    stream.CopyTo(ms);
                    buffer = ms.ToArray();
                }
                finally 
                {
                    stream.Dispose();
                }
            }

            return () => new MemoryStream(buffer);
        }
    }
}