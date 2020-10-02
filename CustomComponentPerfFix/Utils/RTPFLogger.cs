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

        private static readonly ILog _logger = Logger.GetLogger("RogueTechPerfFixes", LogLevel.Debug);

        private const string CRITICAL_LOG_NAME = "CriticalLog.txt";

        private static SpinLock _writeLock = new SpinLock();

        private static string _criticalLogPath;

        private static readonly RTPFLogger _rtpfLogger = new RTPFLogger();

        public static LogWriter Debug => Mod.Settings.LogDebug ? new LogWriter(Mode.Debug) : null;

        public static LogWriter Error => Mod.Settings.LogError ? new LogWriter(Mode.Error) : null;

        public static LogWriter Warning => Mod.Settings.LogWarning ? new LogWriter(Mode.Warning) : null;

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

        public enum Mode
        {
            Debug,
            Warning,
            Error,
        }

        private Mode _mode;

        private static string FormatMessage(string message)
        {
            return $"{LOG_HEADER} {message}";
        }

        public class LogWriter
        {
            private Mode _mode { get; set; }

            public LogWriter(Mode mode)
            {
                _mode = mode;
            }

            public void Write(string message)
            {
                bool refLock = false;
                try
                {
                    _writeLock.Enter(ref refLock);
                    if (refLock)
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
                        LogCritical($"Can't obtain lock for logging.");
                    }
                }
                catch (Exception e)
                {
                    LogCritical(e.ToString());
                }
                finally
                {
                    if (refLock)
                        _writeLock.Exit();
                }
            }
        }
    }
}
