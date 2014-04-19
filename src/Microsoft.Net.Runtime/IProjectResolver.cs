
using System.Collections.Generic;

namespace Microsoft.Net.Runtime
{
    public interface IProjectResolver
    {
        IList<string> SearchPaths { get; }

        bool TryResolveProject(string name, out Project project);
    }
}
