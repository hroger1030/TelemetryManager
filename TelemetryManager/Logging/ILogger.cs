using System;
using System.Threading.Tasks;

namespace TelemetryManager
{
    public interface ILogger : IDisposable
    {
        bool IsDebugEnabled { get; }
        bool IsErrorEnabled { get; }
        bool IsFatalEnabled { get; }
        bool IsInfoEnabled { get; }
        bool IsWarnEnabled { get; }

        Task Debug(string message, Exception ex = null, object data = null);
        Task Info(string message, Exception ex = null, object data = null);
        Task Warn(string message, Exception ex = null, object data = null);
        Task Error(string message, Exception ex = null, object data = null);
        Task Fatal(string message, Exception ex = null, object data = null);
    }
}