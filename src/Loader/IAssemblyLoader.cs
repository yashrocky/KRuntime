﻿using System.Reflection;

namespace Loader
{

    public interface IAssemblyLoader
    {
        Assembly Load(LoadOptions options);
    }

}