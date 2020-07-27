using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using log4net;
using log4net.Config;

namespace TelemetryManager
{
    public class Log4NetLogger : ILogger
    {
        private const string LOG4NET_CONFIG_FILENAME = "log.config";

        private static readonly object _Lock = new object();
        private static bool _Configured;

        private readonly ILog _Log;
        private readonly string _ApplicationName;
        private readonly string _Environment;
        private bool _isDisposed;

        public bool IsDebugEnabled => _Log.IsDebugEnabled;
        public bool IsErrorEnabled => _Log.IsErrorEnabled;
        public bool IsFatalEnabled => _Log.IsFatalEnabled;
        public bool IsInfoEnabled => _Log.IsInfoEnabled;
        public bool IsWarnEnabled => _Log.IsWarnEnabled;

        /// <summary>
        /// CTOR for class that takes in a configuration collection
        /// </summary>
        public Log4NetLogger(Type type, IDictionary<string, string> config) : this(type.FullName, config[TelemetryConfig.APP_NAME], config[TelemetryConfig.ENVIRONMENT]) { }

        /// <summary>
        /// CTOR for class that takes in a configuration collection
        /// </summary>
        public Log4NetLogger(string type, IDictionary<string, string> config) : this(type, config[TelemetryConfig.APP_NAME], config[TelemetryConfig.ENVIRONMENT]) { }

        /// <summary>
        /// CTOR to init logger. This must be called before any logging can be done.
        /// </summary>
        /// <param name="type">The type of the class that the logging is being done in. Use typeof(<your_class>) to pass this parameter in.</param>
        /// <param name="applicationName">A string that denotes the name of the application that you are working in.</param>
        /// <param name="environment">A string denoting the environment that you are working in. Typically this will be prod|stage|dev.</param>
        /// <param name="UseConfigurationSettings">This flag tells log4net were to look to find its appender configuration settings. This will
        /// default to false unless you specify otherwise. We are generally going to want to keep these settings in configuration files over external files.</param>
        public Log4NetLogger(Type type, string applicationName, string environment, bool UseConfigurationSettings = false) : this(type.FullName, applicationName, environment, UseConfigurationSettings) { }

        /// <summary>
        /// CTOR to init logger. This must be called before any logging can be done.
        /// </summary>
        /// <param name="type">The string that describes the module that the logging is being done in. This is an alternative to passing in the logging class type</param>
        /// <param name="applicationName">A string that denotes the name of the application that you are working in.</param>
        /// <param name="environment">A string denoting the environment that you are working in. Typically this will be prod|stage|dev.</param>
        /// <param name="UseConfigurationSettings">This flag tells log4net to use app.config/web.config settings instead of the defaults located in this projects log.config if set to true you need to add the log4net appenders to your app.config/web.config</param>
        public Log4NetLogger(string type, string applicationName, string environment, bool UseConfigurationSettings = false)
        {
            if (string.IsNullOrWhiteSpace(type))
                throw new ArgumentNullException("Type cannot be null or empty");

            if (string.IsNullOrWhiteSpace(applicationName))
                throw new ArgumentNullException("Application name cannot be null or empty");

            if (string.IsNullOrWhiteSpace(environment))
                throw new ArgumentNullException("Environment cannot be null or empty");

            // We want to be able to create multiple loggers for local use, but we only need to set a
            // single watch on the configuration file name. Using singleton lock model to insure init.
            // other class init steps can occur outside of singleton locks.

            if (!_Configured)
            {
                lock (_Lock)
                {
                    if (!_Configured)
                    {
                        var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());

                        if (UseConfigurationSettings)
                        {
                            XmlConfigurator.Configure(logRepository);
                        }
                        else
                        {
                            // depending on the project type, there are differing locations where we need to look for the bits.
                            // 1st check, look in local directory to see if we are we running as a windows app?
                            string fullPath = LOG4NET_CONFIG_FILENAME;

                            if (!File.Exists(fullPath))
                            {
                                // could not find the file here, looks to see if we are dealing with a web app
                                var location = Assembly.GetEntryAssembly().Location;
                                fullPath = Path.GetDirectoryName(location) + "\\" + LOG4NET_CONFIG_FILENAME;

                                if (!File.Exists(fullPath))
                                {
                                    // we didn't find it. we can add additional checks
                                    // here, or just give up and abort the application.

                                    throw new FileNotFoundException($"Failed to find log4net config file '{LOG4NET_CONFIG_FILENAME}' in '{fullPath}'");
                                }
                            }

                            var fileInfo = new FileInfo(fullPath);

                            if (!fileInfo.Exists)
                                throw new FileNotFoundException($"Failed to acquire file info for '{fullPath}'");

                            XmlConfigurator.ConfigureAndWatch(logRepository, fileInfo);
                        }

                        _Configured = true;
                    }
                }
            }

            _isDisposed = false;
            _Log = LogManager.GetLogger(Assembly.GetCallingAssembly(), type);

            // for the sake of consistency, we are using lower case names
            _ApplicationName = applicationName.ToLower();
            _Environment = environment.ToLower();

            // check to make sure that loggers were properly loaded. If all of the output
            // levels are false, we probably failed to find and load a config file.
            if (!_Log.IsDebugEnabled && !_Log.IsErrorEnabled && !_Log.IsFatalEnabled && !_Log.IsInfoEnabled && !_Log.IsWarnEnabled)
                throw new Exception("None of the debug levels are enabled, you probably need to check that the logger configuration has been properly loaded");
        }

        /// <summary>
        /// Core method for logging. Insures that all logged messages meet the criteria defined by LogBase
        /// </summary>
        private void LogMessage(LogBase message, LoggingLevel loggingLevel)
        {
            if (message == null)
            {
                return;
            }

            message.ApplicationName = _ApplicationName;
            message.Environment = _Environment;

            switch (loggingLevel)
            {
                case LoggingLevel.Debug:
                    if (_Log.IsDebugEnabled)
                        _Log.Debug(message);
                    break;

                case LoggingLevel.Info:
                    if (_Log.IsInfoEnabled)
                        _Log.Info(message);
                    break;

                case LoggingLevel.Warn:
                    if (_Log.IsWarnEnabled)
                        _Log.Warn(message);
                    break;

                case LoggingLevel.Error:
                    if (_Log.IsErrorEnabled)
                        _Log.Error(message);
                    break;

                case LoggingLevel.Fatal:
                    if (_Log.IsFatalEnabled)
                        _Log.Fatal(message);
                    break;

                default:
                    // do nothing, logger should not ever throw;
                    break;
            }
        }

        public void Debug(string message) => Debug(new TextMessage(message));

        public void Info(string message) => Info(new TextMessage(message));

        public void Warn(string message) => Warn(new TextMessage(message));

        public void Warn(string message, Exception ex) => Warn(new ErrorMessage(message, ex));

        public void Warn(Exception ex) => Warn(new ErrorMessage(ex.Message, ex));

        public void Error(string message) => Error(new TextMessage(message));

        public void Error(string message, Exception ex) => Error(new ErrorMessage(message, ex));
        
        public void Error(Exception ex) => Error(new ErrorMessage(ex.Message, ex));

        public void Fatal(string message) => Fatal(new TextMessage(message));

        public void Fatal(string message, Exception ex) => Fatal(new ErrorMessage(message, ex));

        public void Fatal(Exception ex) => Fatal(new ErrorMessage(ex.Message, ex));

        //Generic logging:
        public void Debug(LogBase message) => LogMessage(message, LoggingLevel.Debug);

        public void Info(LogBase message) => LogMessage(message, LoggingLevel.Info);

        public void Warn(LogBase message) => LogMessage(message, LoggingLevel.Warn);

        public void Error(LogBase message) => LogMessage(message, LoggingLevel.Error);

        public void Fatal(LogBase message) => LogMessage(message, LoggingLevel.Fatal);

        protected void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // dispose managed objects.
                    _Log.Logger.Repository.Shutdown();
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
