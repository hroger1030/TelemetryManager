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

        void Warn(string message);
        void Warn(string message, Exception ex);
        void Warn(Exception ex);

        void Error(string message);
        void Error(string message, Exception ex);
        void Error(Exception ex);

        void Fatal(string message);
        void Fatal(string message, Exception ex);
        void Fatal(Exception ex);

        void Debug(LogBase message);
        void Info(LogBase message);
        void Warn(LogBase message);
        void Error(LogBase message);
        void Fatal(LogBase message);
    }
}