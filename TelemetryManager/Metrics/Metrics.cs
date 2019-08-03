using System;
using StatsdClient;

namespace TelemetryManager
{
    public class Metrics : IMetrics
    {
        private const int MIN_PORT = 1;
        private const int MAX_PORT = 65535;
        private const string HOST_ADDRESS = "0.0.0.0";
        private const int HOST_PORT = 8125;

        private Statsd _Connection;
        private string _ApplicationName;
        private string _Environment;
        private string _MetricPrefixCache;

        public Metrics(string applicationName, string environment) : this(HOST_ADDRESS, HOST_PORT, applicationName, environment) { }

        public Metrics(string serverAddress, int portNumber, string applicationName, string environment)
        {
            if (string.IsNullOrWhiteSpace(serverAddress))
                throw new ArgumentNullException("Server address cannot be null");

            if (portNumber < MIN_PORT || portNumber > MAX_PORT)
                throw new ArgumentNullException($"Port number '{portNumber}' is outside of legal port range of {MIN_PORT} - {MAX_PORT}");

            if (string.IsNullOrWhiteSpace(applicationName))
                throw new ArgumentNullException("Application name cannot be null or empty");

            if (string.IsNullOrWhiteSpace(environment))
                throw new ArgumentNullException("Environment cannot be null or empty");

            var statsdServer = new StatsdUDP(serverAddress, portNumber);
            _Connection = new Statsd(statsdServer);
            _ApplicationName = applicationName.Replace(" ", string.Empty);
            _Environment = environment.Replace(" ", string.Empty);
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
            var metricName = FormatMetricName(eventName);
            _Connection.Send<Statsd.Counting, int>(metricName, count);
        }

        public void SetGauge(string eventName, double value)
        {
            var metricName = FormatMetricName(eventName);
            _Connection.Send<Statsd.Gauge, double>(metricName, value);
        }

        public void LogDuration(string eventName, double duration)
        {
            var metricName = FormatMetricName(eventName);
            _Connection.Send<Statsd.Timing, double>(metricName, duration);
        }

        public void LogDurationInMs(string eventName, TimeSpan duration)
        {
            LogDuration(eventName, duration.TotalMilliseconds);
        }

        public void LogDurationInMs(string eventName, DateTime start, DateTime end)
        {
            if (end < start)
            {
                var buffer = end;
                end = start;
                start = buffer;
            }

            LogDuration(eventName, (end - start).TotalMilliseconds);
        }

        public void LogDurationInMs(string eventName, Action method)
        {
            var metricName = FormatMetricName(eventName);
            _Connection.Send(method, metricName);
        }

        private string FormatMetricName(string eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName))
                throw new ArgumentNullException("Event name cannot be null or empty");

            if (eventName.IndexOf(' ') != -1)
                throw new ArgumentNullException("Event name should contain no spaces");

            if (_MetricPrefixCache == null)
                _MetricPrefixCache = $"{_Environment}.{Environment.MachineName}.{_ApplicationName}.";

            return _MetricPrefixCache + eventName;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected void Dispose(bool safeToFreeManagedObjects)
        {
            if (safeToFreeManagedObjects)
            {
                _Connection = null;
                _ApplicationName = null;
                _Environment = null;
                _MetricPrefixCache = null;
            }
        }
    }
}
