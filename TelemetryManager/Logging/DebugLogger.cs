using System;

namespace TelemetryManager
{
    /// <summary>
    /// Debugger logger that will write all errors directly to debug console.
    /// </summary>
    public class DebugLogger : ILogger, IDisposable
    {
        private readonly string _ApplicationName;
        private readonly string _Environment;
        private bool _isDisposed;

        public bool IsDebugEnabled { get; set; }
        public bool IsErrorEnabled { get; set; }
        public bool IsFatalEnabled { get; set; }
        public bool IsInfoEnabled { get; set; }
        public bool IsWarnEnabled { get; set; }

        /// <summary>
        /// CTOR to initialize logger. This must be called before any logging can be done.
        /// </summary>
        /// <param name="type">The type of the class that the logging is being done in. Use typeof(<your_class>) to pass this parameter in.</param>
        /// <param name="applicationName">A string that denotes the name of the application that you are working in.</param>
        /// <param name="environment">A string denoting the environment that you are working in. Typically this will be prod|stage|dev.</param>
        public DebugLogger(Type type, string applicationName, string environment) : this(type.FullName, applicationName, environment) { }

        /// <summary>
        /// CTOR to initialize logger. This must be called before any logging can be done.
        /// </summary>
        /// <param name="type">The string that describes the module that the logging is being done in. This is an alternative to passing in the logging class type</param>
        /// <param name="applicationName">A string that denotes the name of the application that you are working in.</param>
        /// <param name="environment">A string denoting the environment that you are working in. Typically this will be prod|stage|dev.</param>
        public DebugLogger(string type, string applicationName, string environment)
        {
            if (string.IsNullOrWhiteSpace(type))
                throw new ArgumentNullException("Type cannot be null or empty");

            if (string.IsNullOrWhiteSpace(applicationName))
                throw new ArgumentNullException("Application name cannot be null or empty");

            if (string.IsNullOrWhiteSpace(environment))
                throw new ArgumentNullException("Environment cannot be null or empty");

            // for the sake of consistency, we are using lower case names
            _ApplicationName = applicationName.ToLower();
            _Environment = environment.ToLower();
            _isDisposed = false;
        }

        public void LogMessage(LoggingLevel loggingLevel, string message, Exception ex, object data)
        {
            var payload = new
            {
                Message = message,
                ApplicationName = _ApplicationName,
                Environment = _Environment,
                Data = data,
            };

            switch (loggingLevel)
            {
                case LoggingLevel.Debug:
                    if (IsDebugEnabled) 
                        System.Diagnostics.Debug.WriteLine($"{loggingLevel}:{message} {ex} {payload}");
                    break;

                case LoggingLevel.Info:
                    if (IsInfoEnabled) 
                        System.Diagnostics.Debug.WriteLine($"{loggingLevel}:{message} {ex} {payload}");
                    break;

                case LoggingLevel.Warn:
                    if (IsWarnEnabled) 
                        System.Diagnostics.Debug.WriteLine($"{loggingLevel}:{message} {ex} {payload}");
                    break;

                case LoggingLevel.Error:
                    if (IsErrorEnabled) 
                        System.Diagnostics.Debug.WriteLine($"{loggingLevel}:{message} {ex} {payload}");
                    break;

                case LoggingLevel.Fatal:
                    if (IsFatalEnabled) 
                        System.Diagnostics.Debug.WriteLine($"{loggingLevel}:{message} {ex} {payload}");
                    break;

                default:
                    System.Diagnostics.Debug.WriteLine($"Unknown logging level encountered: '{loggingLevel}' with  {payload}");
                    break;
            }
        }

        public void Debug(string message) => LogMessage(LoggingLevel.Debug, message, null, null);
        public void Debug(string message, Exception ex, object data) => LogMessage(LoggingLevel.Debug, message, null, null);

        public void Info(string message) => LogMessage(LoggingLevel.Info, message, null, null);
        public void Info(string message, Exception ex, object data) => LogMessage(LoggingLevel.Info, message, null, null);

        public void Warn(string message) => LogMessage(LoggingLevel.Warn, message, null, null);
        public void Warn(string message, Exception ex, object data) => LogMessage(LoggingLevel.Warn, message, null, null);

        public void Error(string message) => LogMessage(LoggingLevel.Error, message, null, null);
        public void Error(string message, Exception ex, object data) => LogMessage(LoggingLevel.Error, message, null, null);

        public void Fatal(string message) => LogMessage(LoggingLevel.Fatal, message, null, null);
        public void Fatal(string message, Exception ex, object data) => LogMessage(LoggingLevel.Fatal, message, null, null);

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // no managed resources to dispose
                }

                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
