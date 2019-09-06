using System;

namespace TelemetryManager
{
    public class DebugLogger : ILogger
    {
        public bool IsDebugEnabled { get; set; }
        public bool IsErrorEnabled { get; set; }
        public bool IsFatalEnabled { get; set; }
        public bool IsInfoEnabled { get; set; }
        public bool IsWarnEnabled { get; set; }

        public DebugLogger()
        {
            System.Diagnostics.Debug.WriteLine($"Initialized DebugLogger");
        }

        public void Debug(string message)
        {
            System.Diagnostics.Debug.WriteLine($"Debug: {message}");
        }

        public void Info(string message)
        {
            System.Diagnostics.Debug.WriteLine($"Info: {message}");
        }

        public void Warn(string message)
        {
            System.Diagnostics.Debug.WriteLine($"Warn: {message}");
        }

        public void Warn(string message, Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Warn: {message}, {ex}");
        }

        public void Error(string message)
        {
            System.Diagnostics.Debug.WriteLine($"Error: {message}");
        }

        public void Error(string message, Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error: {message}, {ex}");
        }

        public void Fatal(string message)
        {
            System.Diagnostics.Debug.WriteLine($"Fatal: {message}");
        }

        public void Fatal(string message, Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fatal: {message}, {ex}");
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected void Dispose(bool safeToFreeManagedObjects) { }
    }
}
