using System;

namespace TelemetryManager
{
    public interface ILogger : IDisposable
    {
        bool IsDebugEnabled { get; }
        bool IsErrorEnabled { get; }
        bool IsFatalEnabled { get; }
        bool IsInfoEnabled { get; }
        bool IsWarnEnabled { get; }

        void Debug(string message);
        void Info(string message);
        void Warn(string message, Exception ex);
        void Error(string message, Exception ex);
        void Fatal(string message, Exception ex);
    }
}