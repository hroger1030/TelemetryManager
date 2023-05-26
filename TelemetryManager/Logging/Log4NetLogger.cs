using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Core;
using log4net.loggly;
using log4net.Repository.Hierarchy;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace TelemetryManager
{
    public class Log4NetLogger : ILogger, IDisposable
    {
        private const string LOG4NET_CONFIG_FILENAME = "log.config";

        private static readonly object _Lock = new();
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
        /// CTOR to initialize logger. This must be called before any logging can be done.
        /// </summary>
        /// <param name="type">The type of the class that the logging is being done in. Use typeof(<your_class>) to pass this parameter in.</param>
        /// <param name="applicationName">A string that denotes the name of the application that you are working in.</param>
        /// <param name="environment">A string denoting the environment that you are working in. Typically this will be prod|stage|dev.</param>
        /// <param name="UseConfigurationSettings">This flag tells log4net were to look to find its appender configuration settings. This will
        /// default to false unless you specify otherwise. We are generally going to want to keep these settings in configuration files over external files.</param>
        public Log4NetLogger(Type type, string applicationName, string environment, bool UseConfigurationSettings = false) : this(type.FullName, applicationName, environment, UseConfigurationSettings) { }

        /// <summary>
        /// CTOR to initialize logger. This must be called before any logging can be done.
        /// </summary>
        /// <param name="type">The string that describes the module that the logging is being done in. This is an alternative to passing in the logging class type</param>
        /// <param name="applicationName">A string that denotes the name of the application that you are working in.</param>
        /// <param name="environment">A string denoting the environment that you are working in. Typically this will be prod|stage|dev.</param>
        /// <param name="UseConfigurationSettings">This flag tells log4net to use app.config/web.config settings instead of the defaults located in this projects log.config if set to true you need to add the log4net appenders to your app.config/web.config</param>
        public Log4NetLogger(string type, string applicationName, string environment, bool UseConfigurationSettings = false)
        {
            if (string.IsNullOrWhiteSpace(type))
                throw new ArgumentNullException(nameof(type));

            if (string.IsNullOrWhiteSpace(applicationName))
                throw new ArgumentNullException(nameof(applicationName));

            if (string.IsNullOrWhiteSpace(environment))
                throw new ArgumentNullException(nameof(environment));

            // We want to be able to create multiple loggers for local use, but we only need to set a
            // single watch on the configuration file name. Using singleton lock model to insure initialize.
            // other class initialize steps can occur outside of singleton locks.

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

            // for the sake of consistency, we are using lower case names
            _ApplicationName = applicationName.ToLower();
            _Environment = environment.ToLower();

            _isDisposed = false;
            _Log = LogManager.GetLogger(Assembly.GetCallingAssembly(), type.ToLower());

            // check to make sure that loggers were properly loaded. If all of the output
            // levels are false, we probably failed to find and load a config file.
            if (!_Log.IsDebugEnabled && !_Log.IsErrorEnabled && !_Log.IsFatalEnabled && !_Log.IsInfoEnabled && !_Log.IsWarnEnabled)
                throw new Exception("None of the debug levels are enabled, you probably need to check that the logger configuration has been properly loaded");
        }

        /// <summary>
        /// Core method for logging. Insures that all logged messages meet the criteria defined by LogBase
        /// </summary>
        public async Task LogMessage(LoggingLevel loggingLevel, string message, Exception ex, object data)
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
                    if (_Log.IsDebugEnabled)
                        _Log.Debug(payload, ex);
                    break;

                case LoggingLevel.Info:
                    if (_Log.IsInfoEnabled)
                        _Log.Info(payload, ex);
                    break;

                case LoggingLevel.Warn:
                    if (_Log.IsWarnEnabled)
                        _Log.Warn(payload, ex);
                    break;

                case LoggingLevel.Error:
                    if (_Log.IsErrorEnabled)
                        _Log.Error(payload, ex);
                    break;

                case LoggingLevel.Fatal:
                    if (_Log.IsFatalEnabled)
                        _Log.Fatal(payload, ex);
                    break;

                default:
                    // do nothing, logger should not ever throw;
                    System.Diagnostics.Debug.WriteLine("Invalid logging level specified");
                    break;
            }

            await Task.CompletedTask;
        }

        public async Task Debug(string message, Exception exception = null, object data = null) =>
            await LogMessage(LoggingLevel.Debug, message, exception, data);

        public async Task Info(string message, Exception exception = null, object data = null) =>
            await LogMessage(LoggingLevel.Info, message, exception, data);

        public async Task Warn(string message, Exception exception = null, object data = null) =>
            await LogMessage(LoggingLevel.Warn, message, exception, data);

        public async Task Error(string message, Exception exception = null, object data = null) =>
            await LogMessage(LoggingLevel.Error, message, exception, data);

        public async Task Fatal(string message, Exception exception = null, object data = null)
            => await LogMessage(LoggingLevel.Fatal, message, exception, data);

        public async Task SetLocalLoggingLevel(LoggingLevel newLevel)
        {
            var logger = (Logger)_Log.Logger;

            switch (newLevel)
            {
                case LoggingLevel.Debug:
                    logger.Level = Level.Debug;
                    break;

                case LoggingLevel.Info:
                    logger.Level = Level.Info;
                    break;

                case LoggingLevel.Warn:
                    logger.Level = Level.Warn;
                    break;

                case LoggingLevel.Error:
                    logger.Level = Level.Error;
                    break;

                case LoggingLevel.Fatal:
                    logger.Level = Level.Fatal;
                    break;

                default:
                    throw new ArgumentOutOfRangeException($"LoggingLevel set to unknown value of '{newLevel}'");
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Not included as part of Ilogger interface, concept of appenders might not 
        /// make sense in all implementations. Also, some appenders cannot be enabled
        /// programatically. (ManagedColoredConsoleAppender)
        /// </summary>
        public async Task EnableAppender(Appender appenderType)
        {
            Logger _logger = (Logger)_Log.Logger;

            switch (appenderType)
            {
                case Appender.FileAppender:
                    _logger.AddAppender(new FileAppender());
                    break;

                case Appender.ManagedColoredConsoleAppender:
                    _logger.AddAppender(new ManagedColoredConsoleAppender());
                    break;

                case Appender.LogglyAppender:
                    _logger.AddAppender(new LogglyAppender());
                    break;

                case Appender.ConsoleAppender:
                    _logger.AddAppender(new ConsoleAppender());
                    break;

                default:
                    throw new ArgumentOutOfRangeException($"Appender set to unknown value of '{appenderType}'");
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Not included as part of Ilogger interface, concept of appenders might not 
        /// make sense in all implementations. Also, some appenders cannot be enabled
        /// programatically. (ManagedColoredConsoleAppender)
        /// </summary>
        public async Task DisableAppender(Appender appenderType)
        {
            Logger _logger = (Logger)_Log.Logger;
            _logger.RemoveAppender(appenderType.ToString());
            await Task.CompletedTask;
        }

        private void Dispose(bool disposing)
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
            GC.SuppressFinalize(this);
        }
    }
}
