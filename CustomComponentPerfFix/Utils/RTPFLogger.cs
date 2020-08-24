using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BattleTech.Rendering;
using CustomUnits;
using HBS.Logging;
using JetBrains.Annotations;

namespace RogueTechPerfFixes
{
    public class RTPFLogger
    {
        private const string LOG_HEADER = "[RTPF]";

        private static SpinLock _spinLock = new SpinLock();

        private static readonly ILog _logger = Logger.GetLogger("RogueTechPerfFixes", LogLevel.Debug);

        private const string CRITICAL_LOG_NAME = "CriticalLog.txt";

        private static string _criticalLogPath;

        private static readonly RTPFLogger _rtpfLogger = new RTPFLogger();

        public static RTPFLogger Debug => Mod.Settings.LogDebug ? _rtpfLogger.SetMode(Mode.Debug) : null;

        public static RTPFLogger Error => Mod.Settings.LogError ? _rtpfLogger.SetMode(Mode.Error) : null;

        public static RTPFLogger Warning => Mod.Settings.LogWarning ? _rtpfLogger.SetMode(Mode.Warning) : null;

        public static void InitCriticalLogger(string modDirectory)
        {
            _criticalLogPath = Path.Combine(modDirectory, CRITICAL_LOG_NAME);
            try
            {
                if (File.Exists(_criticalLogPath))
                    File.Delete(_criticalLogPath);

            }
            catch (Exception e)
            {
                _logger.LogError($"Can't initialize logger for RTPF\n {e}");
            }
        }

        public static void LogCritical(string message)
        {
            File.AppendAllText(_criticalLogPath, $"[{DateTime.Now}] {message}");
        }

        private RTPFLogger()
        {
        }

        private enum Mode
        {
            Debug,
            Warning,
            Error,
        }

        private Mode _mode;

        public void Write(string message)
        {
            bool acquiredLock = false;
            try
            {
                _spinLock.Enter(ref acquiredLock);
                if (acquiredLock)
                {
                    switch (_mode)
                    {
                        case Mode.Debug:
                            _logger.LogDebug(FormatMessage(message));
                            break;
                        case Mode.Error:
                            _logger.LogError(FormatMessage(message));
                            break;
                        case Mode.Warning:
                            _logger.LogWarning(FormatMessage(message));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else
                {
                    LogCritical($"Can't acquire lock for writing log.\n");
                }
            }
            finally
            {
                if (acquiredLock)
                    _spinLock.Exit();
            }
        }

        private static string FormatMessage(string message)
        {
            return $"{LOG_HEADER} {DateTime.Now} {message}";
        }

        private RTPFLogger SetMode(Mode mode)
        {
            _mode = mode;
            return this;
        }

    }
}
