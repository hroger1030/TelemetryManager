using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TelemetryManager
{
    /// <summary>
    /// Debugger logger that will write all errors directly to debug console.
    /// </summary>
    public class DebugLogger : ILogWriter, IDisposable
    {
        private const string DEFAULT_ENVIRONMENT = "dev";

        private readonly string _ApplicationName;
        private readonly string _Environment;
        private bool _isDisposed;

        /// <summary>
        /// Property created to store any logged messages. Used by unit tests to view was was reported.
        /// </summary>
        public List<LogMessage> LoggedMessages { get; private set; } = new();

        public bool IsDebugEnabled { get; set; } = true;
        public bool IsErrorEnabled { get; set; } = true;
        public bool IsFatalEnabled { get; set; } = true;
        public bool IsInfoEnabled { get; set; } = true;
        public bool IsWarnEnabled { get; set; } = true;

        /// <summary>
        /// CTOR to initialize logger. This must be called before any logging can be done.
        /// </summary>
        /// <param name="type">The type of the class that the logging is being done in. Use typeof(<your_class>) to pass this parameter in.</param>
        /// <param name="applicationName">A string that denotes the name of the application that you are working in.</param>
        /// <param name="environment">A string denoting the environment that you are working in. Typically this will be prod|stage|dev.</param>
        public DebugLogger(Type type, string applicationName, string environment = DEFAULT_ENVIRONMENT) : this(type.FullName, applicationName, environment) { }

        /// <summary>
        /// CTOR to initialize logger. This must be called before any logging can be done.
        /// </summary>
        /// <param name="type">The string that describes the module that the logging is being done in. This is an alternative to passing in the logging class type</param>
        /// <param name="applicationName">A string that denotes the name of the application that you are working in.</param>
        /// <param name="environment">A string denoting the environment that you are working in. Typically this will be prod|stage|dev.</param>
        public DebugLogger(string type, string applicationName, string environment = DEFAULT_ENVIRONMENT)
        {
            if (string.IsNullOrWhiteSpace(type))
                throw new ArgumentNullException(nameof(type));

            if (string.IsNullOrWhiteSpace(applicationName))
                throw new ArgumentNullException(nameof(applicationName));

            if (string.IsNullOrWhiteSpace(environment))
                throw new ArgumentNullException(nameof(environment));

            // for the sake of consistency, we are using lower case names
            _ApplicationName = applicationName.ToLower();
            _Environment = environment.ToLower();
            _isDisposed = false;
        }

        public void LogMessage(LoggingLevel loggingLevel, string message, Exception ex, object data)
        {
            var payload = new LogMessage()
            {
                ApplicationName = _ApplicationName,
                Environment = _Environment,
                Error = ex,
                Level = loggingLevel,
                Message = (data == null) ? string.Empty : data.ToString(),
            };

            LoggedMessages.Add(payload);
            System.Diagnostics.Debug.WriteLine($"{loggingLevel}:{message} {ex} {payload}");
        }

        public void Debug(string message, Exception ex = null, object data = null)
            => LogMessage(LoggingLevel.Debug, message, ex, data);

        public void Info(string message, Exception ex = null, object data = null)
            => LogMessage(LoggingLevel.Info, message, ex, data);

        public void Warn(string message, Exception ex = null, object data = null)
            => LogMessage(LoggingLevel.Warn, message, ex, data);

        public void Error(string message, Exception ex = null, object data = null)
            => LogMessage(LoggingLevel.Error, message, ex, data);

        public void Fatal(string message, Exception ex = null, object data = null)
            => LogMessage(LoggingLevel.Fatal, message, ex, data);

        public void SetLocalLoggingLevel(LoggingLevel newLevel)
        {
            switch (newLevel)
            {
                case LoggingLevel.Debug:
                    IsDebugEnabled = true;
                    IsInfoEnabled = true;
                    IsWarnEnabled = true;
                    IsErrorEnabled = true;
                    IsFatalEnabled = true;
                    break;

                case LoggingLevel.Info:
                    IsDebugEnabled = false;
                    IsInfoEnabled = true;
                    IsWarnEnabled = true;
                    IsErrorEnabled = true;
                    IsFatalEnabled = true;
                    break;

                case LoggingLevel.Warn:
                    IsDebugEnabled = false;
                    IsInfoEnabled = false;
                    IsWarnEnabled = true;
                    IsErrorEnabled = true;
                    IsFatalEnabled = true;
                    break;

                case LoggingLevel.Error:
                    IsDebugEnabled = false;
                    IsInfoEnabled = false;
                    IsWarnEnabled = false;
                    IsErrorEnabled = true;
                    IsFatalEnabled = true;
                    break;

                case LoggingLevel.Fatal:
                    IsDebugEnabled = false;
                    IsInfoEnabled = false;
                    IsWarnEnabled = false;
                    IsErrorEnabled = false;
                    IsFatalEnabled = true;
                    break;

                default:
                    throw new ArgumentOutOfRangeException($"LoggingLevel set to unknown value of '{newLevel}'");
            }
        }

        private void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            if (disposing)
            {
                LoggedMessages = null;
            }

            _isDisposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task LogMessageAsync(LoggingLevel loggingLevel, string message, Exception ex, object data)
        {
            await Task.Run(() => LogMessage(loggingLevel, message, ex, data));
        }

        public async Task DebugAsync(string message, Exception ex = null, object data = null)
            => await LogMessageAsync(LoggingLevel.Debug, message, ex, data);

        public async Task InfoAsync(string message, Exception ex = null, object data = null)
            => await LogMessageAsync(LoggingLevel.Info, message, ex, data);

        public async Task WarnAsync(string message, Exception ex = null, object data = null)
            => await LogMessageAsync(LoggingLevel.Warn, message, ex, data);

        public async Task ErrorAsync(string message, Exception ex = null, object data = null)
            => await LogMessageAsync(LoggingLevel.Error, message, ex, data);

        public async Task FatalAsync(string message, Exception ex = null, object data = null)
            => await LogMessageAsync(LoggingLevel.Fatal, message, ex, data);

        public async Task SetLocalLoggingLevelAsync(LoggingLevel newLevel)
        {
            await Task.Run(() => SetLocalLoggingLevel(newLevel));
        }
    }
}
