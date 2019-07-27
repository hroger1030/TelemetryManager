using System;
using System.Configuration;
using System.Diagnostics;

using TelemetryManager;

namespace TestApp
{
    public class Program
    {
        public static readonly string ApplicationName = ConfigurationManager.AppSettings["ApplicationName"];
        public static readonly string AppEnvironment = ConfigurationManager.AppSettings["Environment"];

        private static ILogger _Log;

        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += Application_Error;
            var sw = Stopwatch.StartNew();

            // new up a logger instance..
            _Log = new Logger(typeof(Program), ApplicationName, AppEnvironment);

            try
            {
                // Fire off a set of tests
                _Log.Debug("Debug");
                _Log.Info("Info");
                _Log.Warn("Warning", null);

                throw new Exception("Sample Exception");
            }
            catch (Exception ex)
            {
                _Log.Error("Test Error", ex);
                _Log.Fatal("Test Fatal", ex);
            }
            finally
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Total run time: {sw.Elapsed}");
                Console.WriteLine("Press any key to exit...");
                Console.ResetColor();
                Console.ReadKey();

                _Log.Dispose();
            }
        }

        private static void Application_Error(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                Exception ex = (Exception)e.ExceptionObject;

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Fatal error encountered '{ex.Message}', cannot continue");
                Console.ResetColor();

                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();

                Environment.Exit(1);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unhandled application error: {ex.Message}, stacktrace: {ex.StackTrace}");
            }
        }
    }
}
