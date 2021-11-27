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
        void Debug(string message, Exception ex, object data);

        void Info(string message);
        void Info(string message, Exception ex, object data);

        void Warn(string message);
        void Warn(string message, Exception ex, object data);

        void Error(string message);
        void Error(string message, Exception ex, object data);

        void Fatal(string message);
        void Fatal(string message, Exception ex, object data);
    }
}