using System;
using System.Threading.Tasks;

namespace TelemetryManager
{
    public interface ILogWriter : IDisposable
    {
        bool IsDebugEnabled { get; }
        bool IsErrorEnabled { get; }
        bool IsFatalEnabled { get; }
        bool IsInfoEnabled { get; }
        bool IsWarnEnabled { get; }

        void LogMessage(LoggingLevel loggingLevel, string message, Exception ex, object data);
        void Debug(string message, Exception ex = null, object data = null);
        void Info(string message, Exception ex = null, object data = null);
        void Warn(string message, Exception ex = null, object data = null);
        void Error(string message, Exception ex = null, object data = null);
        void Fatal(string message, Exception ex = null, object data = null);
        void SetLocalLoggingLevel(LoggingLevel newLevel);

        Task LogMessageAsync(LoggingLevel loggingLevel, string message, Exception ex, object data);
        Task DebugAsync(string message, Exception ex = null, object data = null);
        Task InfoAsync(string message, Exception ex = null, object data = null);
        Task WarnAsync(string message, Exception ex = null, object data = null);
        Task ErrorAsync(string message, Exception ex = null, object data = null);
        Task FatalAsync(string message, Exception ex = null, object data = null);
        Task SetLocalLoggingLevelAsync(LoggingLevel newLevel);
    }
}