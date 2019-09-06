using System;
using System.Diagnostics;

namespace TelemetryManager
{
    /// <summary>
    /// Sends metrics to debug console.
    /// </summary>
    public class DebugMetrics : IMetrics
    {
        private bool _IsDisposed;

        public DebugMetrics()
        {
            _IsDisposed = false;
        }

        public void IncrementCounter(string eventName)
        {
            IncrementCounter(eventName, 1);
        }

        public void DecrementCounter(string eventName)
        {
            IncrementCounter(eventName, -1);
        }

        public void IncrementCounter(string eventName, int count)
        {
            Debug.WriteLine($"{eventName} Counter Metric: {count}");
        }

        public void SetGauge(string eventName, double value)
        {
            Debug.WriteLine($"{eventName} Gauge Metric: {value}");
        }

        public void LogDuration(string eventName, double duration)
        {
            Debug.WriteLine($"{eventName} Counter Metric: {duration}");
        }

        public void LogDurationInMs(string eventName, TimeSpan duration)
        {
            Debug.WriteLine($"{eventName} Duration Metric: {duration.TotalMilliseconds} ms");
        }

        public void LogDurationInMs(string eventName, DateTime start, DateTime end)
        {
            Debug.WriteLine($"{eventName} Duration Metric: {(end - start).TotalMilliseconds} ms");
        }

        public void LogDurationInMs(string eventName, Action method)
        {
            var sw = Stopwatch.StartNew();
            method();
            sw.Stop();

            Debug.WriteLine($"{eventName} Duration Metric: {sw.ElapsedMilliseconds} ms");
        }

        protected void Dispose(bool disposing)
        {
            if (!_IsDisposed)
            {
                if (disposing)
                {
                    // dispose managed objects
                }

                _IsDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
