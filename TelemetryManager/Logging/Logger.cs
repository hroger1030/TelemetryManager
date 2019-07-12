using System;
using System.IO;

using log4net;
using log4net.Config;

namespace TelemetryManager
{
    public class Logger : ILogger
    {
        private const string _Log4NetConfigFileName = "log.config";

        private static object _Lock = new object();
        private static bool _Configured;

        private ILog _Log;
        private string _ApplicationName;
        private string _Environment;

        public bool IsDebugEnabled { get { return _Log.IsDebugEnabled; } }
        public bool IsErrorEnabled { get { return _Log.IsErrorEnabled; } }
        public bool IsFatalEnabled { get { return _Log.IsFatalEnabled; } }
        public bool IsInfoEnabled { get { return _Log.IsInfoEnabled; } }
        public bool IsWarnEnabled { get { return _Log.IsWarnEnabled; } }

        public Logger(Type type, string applicationName, string environment) : this(type.FullName, applicationName, environment) { }

        public Logger(string type, string applicationName, string environment)
        {
            // We want to be able to create multiple loggers for local use, but we only need to set a 
            // single watch on the configuration file name. Using singleton lock model to insure init.

            if (!_Configured)
            {
                lock (_Lock)
                {
                    if (!_Configured)
                    {
                        XmlConfigurator.ConfigureAndWatch(new FileInfo(_Log4NetConfigFileName));
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
                    _Log.Debug(message);
                    break;

                case LoggingLevel.Info:
                    _Log.Info(message);
                    break;

                case LoggingLevel.Warn:
                    _Log.Warn(message);
                    break;

                case LoggingLevel.Error:
                    _Log.Error(message);
                    break;

                case LoggingLevel.Fatal:
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

        public void Warn(string message, Exception ex)
        {
            if (_Log.IsWarnEnabled)
                LogMessage(new ErrorMessage(message, ex, _ApplicationName, _Environment), LoggingLevel.Warn);
        }

        public void Error(string message, Exception ex)
        {
            if (_Log.IsErrorEnabled)
                LogMessage(new ErrorMessage(message, ex, _ApplicationName, _Environment), LoggingLevel.Error);
        }

        public void Fatal(string message, Exception ex)
        {
            if (_Log.IsFatalEnabled)
                LogMessage(new ErrorMessage(message, ex, _ApplicationName, _Environment), LoggingLevel.Fatal);
        }

        public void Shutdown()
        {
            _Log.Logger.Repository.Shutdown();
        }
    }
}
