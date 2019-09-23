using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace TelemetryManager
{
    /// <summary>
    /// Sends metrics to debug console.
    /// </summary>
    public class DebugMetrics : IMetrics
    {
        private string _ApplicationName;
        private string _Environment;
        private bool _IsDisposed;

        /// <summary>
        /// CTOR for class that takes in a configuration collection
        /// </summary>
        public DebugMetrics(IDictionary<string, string> config) : this(config[TelemetryConfig.APP_NAME], config[TelemetryConfig.ENVIRONMENT]) { }

        public DebugMetrics(string applicationName, string environment)
        {
            if (string.IsNullOrWhiteSpace(applicationName))
                throw new ArgumentNullException("Application name cannot be null or empty");

            if (string.IsNullOrWhiteSpace(environment))
                throw new ArgumentNullException("Environment cannot be null or empty");

            _ApplicationName = applicationName.Replace(" ", string.Empty);
            _Environment = environment.Replace(" ", string.Empty);
            _IsDisposed = false;
        }

        public void IncrementCounter(string eventName, string[] tags)
        {
            IncrementCounter(eventName, 1, tags);
        }

        public void DecrementCounter(string eventName, string[] tags)
        {
            IncrementCounter(eventName, -1, tags);
        }

        public void IncrementCounter(string eventName, int count, string[] tags)
        {
            string fixedTags = string.Empty;

            if (tags != null)
                string.Join(", ", tags);

            Debug.WriteLine($"{_Environment} {_ApplicationName} {eventName} Counter Metric: {count}. Tags: {fixedTags}");
        }

        public void SetGauge(string eventName, double value, string[] tags)
        {
            string fixedTags = string.Empty;

            if (tags != null)
                string.Join(", ", tags);

            Debug.WriteLine($"{_Environment} {_ApplicationName} {eventName} Gauge Metric: {value}. Tags: {fixedTags}");
        }

        public void LogDuration(string eventName, double duration, string[] tags)
        {
            string fixedTags = string.Empty;

            if (tags != null)
                string.Join(", ", tags);

            Debug.WriteLine($"{_Environment} {_ApplicationName} {eventName} Counter Metric: {duration}. Tags: {fixedTags}");
        }

        public void LogDurationInMs(string eventName, TimeSpan duration, string[] tags)
        {
            string fixedTags = string.Empty;

            if (tags != null)
                string.Join(", ", tags);

            Debug.WriteLine($"{_Environment} {_ApplicationName} {eventName} Duration Metric: {duration.TotalMilliseconds} ms. Tags: {string.Join(", ", tags)}");
        }

        public void LogDurationInMs(string eventName, DateTime start, DateTime end, string[] tags = null)
        {
            string fixedTags = string.Empty;

            if (tags != null)
                string.Join(", ", tags);

            Debug.WriteLine($"{_Environment} {_ApplicationName} {eventName} Duration Metric: {(end - start).TotalMilliseconds} ms. Tags: {string.Join(", ", tags)}");
        }

        public void LogDurationInMs(string eventName, Action method, string[] tags = null)
        {
            var sw = Stopwatch.StartNew();
            method();
            sw.Stop();

            string fixedTags = string.Empty;

            if (tags != null)
                string.Join(", ", tags);

            Debug.WriteLine($"{_Environment} {_ApplicationName} {eventName} Duration Metric: {sw.ElapsedMilliseconds} ms. Tags: {string.Join(", ", tags)}");
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
