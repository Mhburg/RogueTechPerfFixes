using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using CustomComponents;

namespace RogueTechPerfFixes
{
    public class CacheManager
    {
        public static Dictionary<MapTerrainDataCell, bool> WatchCache { get; } = new Dictionary<MapTerrainDataCell, bool>();
    }
}
