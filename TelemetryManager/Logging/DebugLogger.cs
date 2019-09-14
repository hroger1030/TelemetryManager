using System;
using System.Collections.Generic;

namespace TelemetryManager
{
    /// <summary>
    /// Debugger logger that will write all errors directly to debug console.
    /// </summary>
    public class DebugLogger : ILogger
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
        /// CTOR for class that takes in a configuration collection
        /// </summary>
        public DebugLogger(Type type, IDictionary<string, string> config) : this(type.FullName, config[TelemetryConfig.APP_NAME], config[TelemetryConfig.ENVIRONMENT]) { }

        /// <summary>
        /// CTOR for class that takes in a configuration collection
        /// </summary>
        public DebugLogger(string type, IDictionary<string, string> config) : this(type, config[TelemetryConfig.APP_NAME], config[TelemetryConfig.ENVIRONMENT]) { }

        /// <summary>
        /// CTOR to init logger. This must be called before any logging can be done.
        /// </summary>
        /// <param name="type">The type of the class that the logging is being done in. Use typeof(<your_class>) to pass this parameter in.</param>
        /// <param name="applicationName">A string that denotes the name of the application that you are working in.</param>
        /// <param name="environment">A string denoting the environment that you are working in. Typically this will be prod|stage|dev.</param>
        public DebugLogger(Type type, string applicationName, string environment) : this(type.FullName, applicationName, environment) { }

        /// <summary>
        /// CTOR to init logger. This must be called before any logging can be done.
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

        private void LogMessage(LogBase message)
        {
            if (message == null)
            {
                return;
            }

            message.ApplicationName = _ApplicationName;
            message.Environment = _Environment;
            System.Diagnostics.Debug.WriteLine($"Debug message: {message}");
        }

        public void Debug(string message) => Debug(new TextMessage(message));

        public void Info(string message) => Info(new TextMessage(message));

        public void Warn(string message) => Warn(new TextMessage(message));

        public void Warn(string message, Exception ex) => Warn(new ErrorMessage(message, ex));

        public void Error(string message) => Error(new TextMessage(message));

        public void Error(string message, Exception ex) => Error(new ErrorMessage(message, ex));

        public void Fatal(string message) => Fatal(new TextMessage(message));

        public void Fatal(string message, Exception ex) => Fatal(new ErrorMessage(message, ex));

        public void Debug(LogBase message)
        {
            if (IsDebugEnabled)
            {
                LogMessage(message);
            }
        }

        public void Info(LogBase message)
        {
            if (IsInfoEnabled)
            {
                LogMessage(message);
            }
        }

        public void Warn(LogBase message)
        {
            if (IsWarnEnabled)
            {
                LogMessage(message);
            }
        }

        public void Error(LogBase message)
        {
            if (IsErrorEnabled)
            {
                LogMessage(message);
            }
        }

        public void Fatal(LogBase message)
        {
            if (IsFatalEnabled)
            {
                LogMessage(message);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // no managed resporces to dispose
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
