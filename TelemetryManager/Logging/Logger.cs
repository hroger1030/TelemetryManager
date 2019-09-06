using System;
using System.IO;
using System.Reflection;

using log4net;
using log4net.Config;

namespace TelemetryManager
{
    public class Logger : ILogger
    {
        private static readonly bool USE_EXTERNAL_LOG_CONFIG = true;
        private const string LOG4NET_CONFIG_FILENAME = "log.config";

        private static object _Lock = new object();
        private static bool _Configured;

        private ILog _Log;
        private string _ApplicationName;
        private string _Environment;

        public bool IsDebugEnabled => _Log.IsDebugEnabled;
        public bool IsErrorEnabled => _Log.IsErrorEnabled;
        public bool IsFatalEnabled => _Log.IsFatalEnabled;
        public bool IsInfoEnabled => _Log.IsInfoEnabled;
        public bool IsWarnEnabled => _Log.IsWarnEnabled;

        public Logger(Type type, string applicationName, string environment) : this(type.FullName, applicationName, environment) { }

        public Logger(string type, string applicationName, string environment)
        {
            if (string.IsNullOrWhiteSpace(applicationName))
                throw new ArgumentNullException("Application name cannot be null or empty");

            if (string.IsNullOrWhiteSpace(environment))
                throw new ArgumentNullException("Environment cannot be null or empty");

            if (!_Configured)
            {
                lock (_Lock)
                {
                    if (!_Configured)
                    {
                        if (USE_EXTERNAL_LOG_CONFIG)
                        {
                            string path = LOG4NET_CONFIG_FILENAME;

                            if (!File.Exists(path))
                            {
                                path = Assembly.GetExecutingAssembly().Location;

                                if (!File.Exists(path))
                                    throw new FileNotFoundException($"Unable to locate file '{LOG4NET_CONFIG_FILENAME}' in path '{path}'");
                            }

                            XmlConfigurator.ConfigureAndWatch(new FileInfo(path));
                        }
                        else
                        {
                            XmlConfigurator.Configure();
                        }

                        _Configured = true;
                    }
                }
            }

            _Log = LogManager.GetLogger(type);

            // for the sake of consistency, we are using lower case names
            _ApplicationName = applicationName.ToLower();
            _Environment = environment.ToLower();
        }

        /// <summary>
        /// Core method for logging. Insures that all logged messages meet the criteria defined by LogBase
        /// </summary>
        private void LogMessage(LogBase message, LoggingLevel loggingLevel)
        {
            switch (loggingLevel)
            {
                case LoggingLevel.Debug:
                    if (IsDebugEnabled)
                        _Log.Debug(message);
                    break;

                case LoggingLevel.Info:
                    if (IsInfoEnabled)
                        _Log.Info(message);
                    break;

                case LoggingLevel.Warn:
                    if (IsWarnEnabled)
                        _Log.Warn(message);
                    break;

                case LoggingLevel.Error:
                    if (IsErrorEnabled)
                        _Log.Error(message);
                    break;

                case LoggingLevel.Fatal:
                    if (IsFatalEnabled)
                        _Log.Fatal(message);
                    break;

                default:
                    // do nothing, logger should not ever throw;
                    break;
            }
        }

        public void Debug(string message)
        {
            if (_Log.IsDebugEnabled)
                LogMessage(new TextMessage(message, _ApplicationName, _Environment), LoggingLevel.Debug);
        }

        public void Info(string message)
        {
            if (_Log.IsInfoEnabled)
                LogMessage(new TextMessage(message, _ApplicationName, _Environment), LoggingLevel.Info);
        }

        public void Warn(string message)
        {
            LogMessage(new TextMessage(message, _ApplicationName, _Environment), LoggingLevel.Warn);
        }

        public void Warn(string message, Exception ex)
        {
            LogMessage(new ErrorMessage(message, ex, _ApplicationName, _Environment), LoggingLevel.Warn);
        }

        public void Error(string message)
        {
            LogMessage(new TextMessage(message, _ApplicationName, _Environment), LoggingLevel.Error);
        }

        public void Error(string message, Exception ex)
        {
            LogMessage(new ErrorMessage(message, ex, _ApplicationName, _Environment), LoggingLevel.Error);
        }

        public void Fatal(string message)
        {
            LogMessage(new TextMessage(message, _ApplicationName, _Environment), LoggingLevel.Fatal);
        }

        public void Fatal(string message, Exception ex)
        {
            LogMessage(new ErrorMessage(message, ex, _ApplicationName, _Environment), LoggingLevel.Fatal);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected void Dispose(bool safeToFreeManagedObjects)
        {
            if (safeToFreeManagedObjects)
            {
                if (_Log != null)
                    _Log.Logger.Repository.Shutdown();

                _Log = null;
                _ApplicationName = null;
                _Environment = null;
                _Lock = null;
            }
        }
    }
}
