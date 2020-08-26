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

        public const string LOG_HEADER = "[PerfFix]";

        public static void CheckExitCounter(string message, int counter)
        {
            int exitCounter = VisibilityCacheGate.GetCounter;
            if (exitCounter > counter)
            {
                RTPFLogger.Error?.Write(message);
                VisibilityCacheGate.ExitAll();
            }
        }
    }
}
