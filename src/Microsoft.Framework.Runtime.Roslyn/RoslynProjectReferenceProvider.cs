using System;
using System.Collections.Generic;

#if ASPNETCORE50
using System.Runtime.Loader;
using System.Reflection;
#else
using System.Diagnostics;
using System.Runtime.Versioning;
#endif

namespace Microsoft.Framework.Runtime.Roslyn
{
    public class RoslynProjectReferenceProvider : IProjectReferenceProvider
    {
        private readonly RoslynCompiler _compiler;

        public RoslynProjectReferenceProvider(
            ICache cache,
            ICacheContextAccessor cacheContextAccessor,
            INamedCacheDependencyProvider namedCacheProvider,
            IFileWatcher watcher,
            IServiceProvider services)
        {
            _compiler = new RoslynCompiler(
                cache,
                cacheContextAccessor,
                namedCacheProvider,
                watcher,
                services);
        }

        public IMetadataProjectReference GetProjectReference(
            Project project,
            ILibraryKey target,
            Func<ILibraryExport> referenceResolver,
            IList<IMetadataReference> outgoingReferences,
            Func<IAssemblyLoadContext> assemblyLoadContextResolver = null)
        {
            var export = referenceResolver();
            var incomingReferences = export.MetadataReferences;
            var incomingSourceReferences = export.SourceReferences;

            var assemblyLoadContext = assemblyLoadContextResolver == null ?
                null : assemblyLoadContextResolver();

            var compliationContext = _compiler.CompileProject(
                project,
                target,
                incomingReferences,
                incomingSourceReferences,
                outgoingReferences,
                assemblyLoadContext);

            if (compliationContext == null)
            {
                return null;
            }

            // Project reference
            return new RoslynProjectReference(compliationContext);
        }
    }
}