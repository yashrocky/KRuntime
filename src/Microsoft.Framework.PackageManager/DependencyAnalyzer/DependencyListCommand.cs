// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Framework.PackageManager.DependencyAnalyzer
{
    internal class DependencyListCommand
    {
        private readonly DependencyListOptions _options;

        public DependencyListCommand(DependencyListOptions options)
        {
            _options = options;
        }

        public bool Execute()
        {
            bool result = true;
            Info("List dependencies for {0} ({1})", _options.Project.Name, _options.Project.ProjectFilePath);

            foreach (var targetFramework in _options.Project.GetTargetFrameworks())
            {
                Info("[Target framework {0}]", targetFramework.FrameworkName.Identifier.ToString());

                var finder = new DependencyFinder(_options, targetFramework.FrameworkName);

                IEnumerable<string> unresolved;
                var dependencies = finder.Find(_options.Local, out unresolved);

                Info("Dependencies: ");
                PrintCollectionInOrder(dependencies);

                if (unresolved.Any())
                {
                    Info("Unresolved dependencies: ");
                    PrintCollectionInOrder(unresolved);

                    result = false;
                }
            }

            return result;
        }

        private void PrintCollectionInOrder(IEnumerable<string> collection, Func<string, string> keySelector = null)
        {
            if (keySelector == null)
            {
                keySelector = one => one;
            }

            var ordered = collection.OrderBy(keySelector).ToArray();
            for (int i = 0; i < ordered.Length; ++i)
            {
                Info("{0,3} > {1}", i, ordered[i]);
            }
        }

        private void Info(string format, params object[] args)
        {
            _options.Reports.Information.WriteLine(format, args);
        }
    }
}