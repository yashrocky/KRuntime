using System;
using System.Reflection;
using Microsoft.Framework.Runtime.Common.DependencyInjection;

namespace Microsoft.Framework.Runtime
{
    public class LanguageServices
    {
        public LanguageServices(string name, TypeInformation projectExportProvider)
        {
            Name = name;
            ProjectReferenceProvider = projectExportProvider;
        }

        public string Name { get; private set; }

        public TypeInformation ProjectReferenceProvider { get; private set; }

        public static T CreateService<T>(IServiceProvider sp, IAssemblyLoadContext context, TypeInformation typeInfo)
        {
            var assembly = context.Load(typeInfo.AssemblyName);

            var type = assembly.GetType(typeInfo.TypeName);

            return (T)ActivatorUtilities.CreateInstance(sp, type);
        }
    }
}