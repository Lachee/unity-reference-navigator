using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lachee.ReferenceNavigator.Search
{
    /// <summary>
    /// Type of asset to search for
    /// </summary>
    [System.Flags]
    public enum SearchType
    {
        None = 0,
        Files = 1 << 0,
        Scene = 1 << 1,
        Prefab = 1 << 2,
        Script = 1 << 3,
        Asset = 1 << 4,
        Material = 1 << 5
    }
}
