// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet;

namespace Microsoft.Framework.PackageManager.Restore.NuGet
{
    public class NuGetv2Feed : IPackageFeed
    {
        private readonly string _baseUri;
        private readonly Reports _reports;
        private readonly HttpSource _httpSource;
        private readonly TimeSpan _cacheAgeLimitList;
        private readonly TimeSpan _cacheAgeLimitNupkg;
        private readonly bool _ignoreFailure;
        private bool _ignored;

        private readonly Dictionary<string, Task<IEnumerable<PackageInfo>>> _packageVersionsCache = new Dictionary<string, Task<IEnumerable<PackageInfo>>>();
        private readonly Dictionary<string, Task<NupkgEntry>> _nupkgCache = new Dictionary<string, Task<NupkgEntry>>();

        public NuGetv2Feed(
            string baseUri,
            string userName,
            string password,
            bool noCache,
            Reports reports,
            bool ignoreFailure)
        {
            _baseUri = baseUri.EndsWith("/") ? baseUri : (baseUri + "/");
            _reports = reports;
            _httpSource = new HttpSource(baseUri, userName, password, reports);
            _ignoreFailure = ignoreFailure;
            if (noCache)
            {
                _cacheAgeLimitList = TimeSpan.Zero;
                _cacheAgeLimitNupkg = TimeSpan.Zero;
            }
            else
            {
                _cacheAgeLimitList = TimeSpan.FromMinutes(30);
                _cacheAgeLimitNupkg = TimeSpan.FromHours(24);
            }
        }

        public Task<IEnumerable<PackageInfo>> FindPackagesByIdAsync(string id)
        {
            lock (_packageVersionsCache)
            {
                Task<IEnumerable<PackageInfo>> task;
                if (_packageVersionsCache.TryGetValue(id, out task))
                {
                    return task;
                }
                return _packageVersionsCache[id] = FindPackagesByIdAsyncCore(id);
            }
        }

        public async Task<IEnumerable<PackageInfo>> FindPackagesByIdAsyncCore(string id)
        {
            for (int retry = 0; retry != 3; ++retry)
            {
                if (_ignored)
                {
                    return new List<PackageInfo>();
                }

                try
                {
                    var uri = _baseUri + "FindPackagesById()?Id='" + id + "'&$select=Id,Version&$format=json";
                    var results = new List<PackageInfo>();
                    var page = 1;
                    while (true)
                    {
                        // TODO: Pages for a package Id are cahced separately.
                        // So we will get inaccurate data when a page shrinks.
                        // However, (1) In most cases the pages grow rather than shrink;
                        // (2) cache for pages is valid for only 30 min.
                        // So we decide to leave current logic and observe.
                        using (var data = await _httpSource.GetAsync(uri,
                        string.Format("list_{0}_json_page{1}", id, page),
                        retry == 0 ? _cacheAgeLimitList : TimeSpan.Zero))
                        {
                            try
                            {
                                string nextUri = ParseFeedEntries(id, results, data);

                                // Stop if there's nothing else to GET
                                if (string.IsNullOrEmpty(nextUri))
                                {
                                    break;
                                }

                                uri = nextUri;
                                page++;
                            }
                            catch (JsonReaderException)
                            {
                                _reports.Information.WriteLine("The file {0} is corrupt",
                                    data.CacheFileName.Yellow().Bold());
                                throw;
                            }
                        }
                    }

                    return results;
                }
                catch (Exception ex)
                {
                    if (retry == 2)
                    {
                        // Fail silently by returning empty result list
                        if (_ignoreFailure)
                        {
                            _ignored = true;
                            _reports.Information.WriteLine(
                                string.Format("Failed to retrieve information from remote source '{0}'".Yellow(),
                                    _baseUri));
                            return new List<PackageInfo>();
                        }

                        _reports.Error.WriteLine(string.Format("Error: FindPackagesById: {1}\r\n  {0}",
                            ex.Message, id.Red().Bold()));
                        throw;
                    }
                    else
                    {
                        _reports.Information.WriteLine(string.Format("Warning: FindPackagesById: {1}\r\n  {0}", ex.Message, id.Yellow().Bold()));
                    }
                }
            }
            return null;
        }

        private string ParseFeedEntries(string id, List<PackageInfo> results, HttpSourceResult data)
        {
            var obj = JObject.Load(new JsonTextReader(new StreamReader(data.Stream)));

            var root = obj["d"];
            var entries = root["results"] as JArray;

            foreach (JObject entry in entries)
            {
                results.Add(BuildPackageInfoFromJsonEntry(id, entry));
            }

            var nextUri = root.Value<string>("__next");

            if (string.IsNullOrEmpty(nextUri))
            {
                return null;
            }

            return nextUri + "&$format=json";
        }

        private PackageInfo BuildPackageInfoFromJsonEntry(string id, JObject entry)
        {
            var info = new PackageInfo
            {
                // If 'Id' element exist, use its value as accurate package Id
                // Use the given Id as final fallback if all elements above don't exist
                Id = entry.Value<string>("Id") ?? id,
                Version = SemanticVersion.Parse(entry.Value<string>("Version")),
                ContentUri = entry["__metadata"].Value<string>("media_src")
            };

            return info;
        }

        public async Task<Stream> OpenNuspecStreamAsync(PackageInfo package)
        {
            return await PackageUtilities.OpenNuspecStreamFromNupkgAsync(package, OpenNupkgStreamAsync, _reports.Information);
        }

        public async Task<Stream> OpenNupkgStreamAsync(PackageInfo package)
        {
            Task<NupkgEntry> task;
            lock (_nupkgCache)
            {
                if (!_nupkgCache.TryGetValue(package.ContentUri, out task))
                {
                    task = _nupkgCache[package.ContentUri] = OpenNupkgStreamAsyncCore(package);
                }
            }
            var result = await task;
            if (result == null)
            {
                return null;
            }

            // Acquire the lock on a file before we open it to prevent this process
            // from opening a file deleted by the logic in HttpSource.GetAsync() in another process
            return await ConcurrencyUtilities.ExecuteWithFileLocked(result.TempFileName, _ =>
            {
                return Task.FromResult(
                    new FileStream(result.TempFileName, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete));
            });
        }

        private async Task<NupkgEntry> OpenNupkgStreamAsyncCore(PackageInfo package)
        {
            for (int retry = 0; retry != 3; ++retry)
            {
                try
                {
                    using (var data = await _httpSource.GetAsync(
                        package.ContentUri,
                        "nupkg_" + package.Id + "." + package.Version,
                        retry == 0 ? _cacheAgeLimitNupkg : TimeSpan.Zero))
                    {
                        return new NupkgEntry
                        {
                            TempFileName = data.CacheFileName
                        };
                    }
                }
                catch (Exception ex)
                {
                    if (retry == 2)
                    {
                        _reports.Error.WriteLine(string.Format("Error: DownloadPackageAsync: {1}\r\n  {0}", ex.Message, package.ContentUri.Red().Bold()));
                    }
                    else
                    {
                        _reports.Information.WriteLine(string.Format("Warning: DownloadPackageAsync: {1}\r\n  {0}".Yellow().Bold(), ex.Message, package.ContentUri.Yellow().Bold()));
                    }
                }
            }
            return null;
        }

        private class NupkgEntry
        {
            public string TempFileName { get; set; }
        }
    }
}