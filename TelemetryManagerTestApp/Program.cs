using System;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;
using TelemetryManager;

namespace TestApp
{
    public class Program
    {
        public static readonly string ApplicationName = ConfigurationManager.AppSettings["ApplicationName"];
        public static readonly string AppEnvironment = ConfigurationManager.AppSettings["Environment"];

        private static ILogger _Log;
        private static IMetrics _Metrics;

        public static async Task Main()
        {
            AppDomain.CurrentDomain.UnhandledException += Application_Error;
            var sw = Stopwatch.StartNew();

            // new up a logger instance..
            _Log = new Log4NetLogger(typeof(Program), ApplicationName, AppEnvironment);
            _Metrics = new DebugMetrics(ApplicationName, AppEnvironment);

            try
            {
                // Fire off a set of tests
                await _Log.DebugAsync("Debug");
                await _Log.InfoAsync("Info");
                await _Log.WarnAsync("Warning");

                for (int i = 0; i < 100; i++)
                    await _Metrics.IncrementCounterAsync("Metric1");

                throw new Exception("Sample Exception");
            }
            catch (Exception ex)
            {
                await _Log.ErrorAsync("Test Error", ex, null);
                await _Log.FatalAsync("Test Fatal", ex, null);
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

                _Log.Fatal("Fatal application error occured", ex, null);
                _Log.Dispose();

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
