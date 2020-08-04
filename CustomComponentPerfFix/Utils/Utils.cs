using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HBS.Logging;

namespace RogueTechPerfFixes
{
    public static class Utils
    {
        public static ILog Logger { get; } = HBS.Logging.Logger.GetLogger("RogueTechPerfFixes", LogLevel.Debug);
    }
}
