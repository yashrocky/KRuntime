// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using NuGet;

namespace Microsoft.Framework.Runtime.FileSystem
{
    public class FileWatcher : IFileWatcher
    {
        private readonly HashSet<IPattern> _patterns = new HashSet<IPattern>();
        private readonly List<IWatcherRoot> _watchers = new List<IWatcherRoot>();

        public FileWatcher()
        {

        }

        public FileWatcher(string path)
        {
            AddWatcher(path);
        }

        public event Action<string> OnChanged;

        public void WatchFile(string path)
        {
            _patterns.Add(new Pattern(path));
        }

        public void WatchFilePatterns(string basePath, IEnumerable<string> includePatterns, IEnumerable<string> excludePatterns)
        {
            var includes = includePatterns.Select(pattern => new Pattern(basePath, pattern));
            var excludes = excludePatterns.Select(pattern => new Pattern(basePath, pattern));
            _patterns.Add(new MultiPattern(includes, excludes));
        }

        public void WatchProject(string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath))
            {
                return;
            }

            // If any watchers already handle this path then noop
            if (!IsAlreadyWatched(projectPath))
            {
                // To reduce the number of watchers we have we add a watcher to the root
                // of this project so that we'll be notified if anything we care
                // about changes
                var rootPath = ProjectResolver.ResolveRootDirectory(projectPath);
                AddWatcher(rootPath);
            }
        }

        // For testing
        internal bool IsAlreadyWatched(string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath))
            {
                return false;
            }

            bool anyWatchers = false;

            foreach (var watcher in _watchers)
            {
                // REVIEW: This needs to work x-platform, should this be case
                // sensitive?
                if (EnsureTrailingSlash(projectPath).StartsWith(EnsureTrailingSlash(watcher.Path), StringComparison.OrdinalIgnoreCase))
                {
                    anyWatchers = true;
                }
            }

            return anyWatchers;
        }

        public void Dispose()
        {
            foreach (var w in _watchers)
            {
                w.Dispose();
            }

            _watchers.Clear();
        }

        public bool ReportChange(string newPath, WatcherChangeTypes changeType)
        {
            return ReportChange(oldPath: null, newPath: newPath, changeType: changeType);
        }

        public bool ReportChange(string oldPath, string newPath, WatcherChangeTypes changeType)
        {
            if (HasChanged(oldPath, newPath, changeType))
            {
                Trace.TraceInformation("[{0}]: HasChanged({1}, {2}, {3})", nameof(FileWatcher), oldPath, newPath, changeType);

                if (OnChanged != null)
                {
                    OnChanged(oldPath ?? newPath);
                }

                return true;
            }

            return false;
        }

        private static string EnsureTrailingSlash(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            if (path[path.Length - 1] != Path.DirectorySeparatorChar)
            {
                return path + Path.DirectorySeparatorChar;
            }

            return path;
        }

        // For testing only
        internal void AddWatcher(IWatcherRoot watcherRoot)
        {
            _watchers.Add(watcherRoot);
        }

        private void AddWatcher(string path)
        {
            var watcher = new FileSystemWatcher(path);
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;

            watcher.Changed += OnWatcherChanged;
            watcher.Renamed += OnRenamed;
            watcher.Deleted += OnWatcherChanged;
            watcher.Created += OnWatcherChanged;

            _watchers.Add(new FileSystemWatcherRoot(watcher));
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            ReportChange(e.OldFullPath, e.FullPath, e.ChangeType);
        }

        private void OnWatcherChanged(object sender, FileSystemEventArgs e)
        {
            ReportChange(e.FullPath, e.ChangeType);
        }

        private bool HasChanged(string oldPath, string newPath, WatcherChangeTypes changeType)
        {
            foreach (var p in _patterns)
            {
                switch (changeType)
                {
                    case WatcherChangeTypes.Created:
                        // (null, c:\foo, created)
                        // (null, c:\foo, changed)
                        if (p.MatchesFile(newPath))
                        {
                            return true;
                        }

                        break;
                    case WatcherChangeTypes.Deleted:
                        // (null, c:\foo.cs, deleted)
                        // (null, c:\foo, deleted)
                        if (p.MatchesFile(newPath) || p.MatchesDirectory(newPath))
                        {
                            return true;
                        }
                        break;
                    case WatcherChangeTypes.Changed:
                        // (null, c:\foo.cs, changed)
                        // (null, c:\foo.cs, created)
                        if (p.MatchesFile(newPath))
                        {
                            return true;
                        }
                        break;
                    case WatcherChangeTypes.Renamed:
                        // (c:\foo, c:\bar, renamed)
                        // (c:\foo.cs, c:\bar.cs, renamed)
                        if (p.MatchesFile(oldPath) || p.MatchesFile(newPath))
                        {
                            return true;
                        }

                        if (p.MatchesDirectory(oldPath) || p.MatchesDirectory(newPath))
                        {
                            return true;
                        }

                        break;
                    default:
                        break;
                }
            }

            return false;
        }

        private interface IPattern
        {
            bool MatchesFile(string path);
            bool MatchesDirectory(string path);
        }

        private class MultiPattern : IPattern
        {
            private readonly IEnumerable<Pattern> _excludes;
            private readonly IEnumerable<Pattern> _includes;

            public MultiPattern(IEnumerable<Pattern> includes, IEnumerable<Pattern> excludes)
            {
                _includes = includes;
                _excludes = excludes;
            }

            public bool MatchesFile(string path)
            {
                return Matches(path, p => p.MatchesFile(path));
            }

            public bool MatchesDirectory(string path)
            {
                return Matches(path, p => p.MatchesDirectory(path));
            }

            private bool Matches(string path, Func<IPattern, bool> matcher)
            {
                bool included = _includes.Any(matcher);
                bool excluded = _excludes.Any() ? _excludes.All(matcher) : false;

                return included && !excluded;
            }
        }

        private class Pattern : IPattern
        {
            private readonly Func<string, bool> _matcher;
            private readonly string _testFile;

            public Pattern(string path)
            {
                FullPattern = path;
                _matcher = p => string.Equals(p, path);
            }

            public Pattern(string basePath, string pattern)
            {
                FullPattern = Path.Combine(basePath, pattern);
                var regex = PathResolver.WildcardToRegex(FullPattern);
                _matcher = p => regex.IsMatch(p);

                // Extract a file pattern from the last segment of the glob pattern
                // **/*.cs
                // **/*.*
                // foo/**/*
                // ../x/y/*/*.cs
                // ../../x/y/foo.cs
                var lastSlash = pattern.LastIndexOfAny(new[] { '/', '\\' });
                if (lastSlash != -1)
                {
                    var lastToken = pattern.Substring(lastSlash + 1);
                    if (PathResolver.IsWildcardSearch(lastToken))
                    {
                        if (lastToken == "**" || lastToken == "*.*" || lastToken == "*")
                        {
                            // any file name
                            _testFile = "test.txt";
                        }
                        else
                        {
                            // file name with extension
                            _testFile = lastToken.Replace("*.", "test.");
                        }
                    }
                }
            }

            public string FullPattern { get; private set; }

            public bool MatchesFile(string path)
            {
                if (string.IsNullOrEmpty(path))
                {
                    return false;
                }

                return _matcher(path);
            }

            public bool MatchesDirectory(string path)
            {
                if (string.IsNullOrEmpty(path))
                {
                    return false;
                }

                if (string.IsNullOrEmpty(_testFile))
                {
                    return false;
                }

                if (Path.HasExtension(path))
                {
                    return false;
                }

                var testPath = Path.Combine(path, _testFile);

                if (_matcher(testPath))
                {
                    return true;
                }

                return false;
            }

            public override bool Equals(object obj)
            {
                var other = obj as Pattern;
                if (other != null)
                {
                    return string.Equals(FullPattern, other.FullPattern);
                }

                return false;
            }

            public override int GetHashCode()
            {
                return FullPattern.GetHashCode();
            }
        }
    }

    public sealed class NoopWatcher : IFileWatcher, IFileMonitor
    {
        public static readonly NoopWatcher Instance = new NoopWatcher();

        private NoopWatcher()
        {

        }

        public void WatchFile(string path)
        {

        }

        public void WatchDirectory(string path)
        {

        }

        public void WatchFilePatterns(string basePath, IEnumerable<string> includePatterns, IEnumerable<string> excludePatterns)
        {

        }

        // Suppressing warning CS0067: The event 'Microsoft.Framework.Runtime.FileSystem.NoopWatcher.OnChanged' is never used
#pragma warning disable 0067
        public event Action<string> OnChanged;
#pragma warning restore 0067

        public void Dispose()
        {
        }

        public void WatchProject(string path)
        {

        }
    }
}
