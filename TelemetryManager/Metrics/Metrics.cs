using System;

using StatsdClient;

namespace TelemetryManager
{
    public class Metrics : IMetrics
    {
        private const int MIN_PORT = 1;
        private const int MAX_PORT = 65535;
        private const int DEFAULT_PORT = 8125;

        private Statsd _Connection;

        public Metrics(string serverAddress, string applicationName) : this(serverAddress, DEFAULT_PORT, applicationName) { }

        public Metrics(string serverAddress, int portNumber, string applicationNames)
        {
            if (string.IsNullOrWhiteSpace(serverAddress))
                throw new ArgumentNullException("Server address cannot be null");

            if (portNumber < MIN_PORT || portNumber > MAX_PORT)
                throw new ArgumentNullException($"Port number '{portNumber}' is outside of legal port range of {MIN_PORT} - {MAX_PORT}");

            var statsdServer = new StatsdUDP(serverAddress, portNumber);
            _Connection = new Statsd(statsdServer);

            //var dogstatsdConfig = new StatsdConfig
            //{
            //    StatsdServerName = serverAddress,
            //    StatsdPort = portNumber,
            //    Prefix = applicationName,
            //    ConstantTags = constTags,
            //};

            //DogStatsd.Configure(dogstatsdConfig);
        }

        public void IncrementCounter(string eventName, int count)
        {
            if (string.IsNullOrWhiteSpace(eventName))
                throw new ArgumentNullException("Event name cannot be null");

            //DogStatsd.Increment(eventName, count);
            _Connection.Send<Statsd.Counting, int>(eventName, count);
        }

        public void DecrementCounter(string eventName, int count)
        {
            IncrementCounter(eventName, -count);
        }

        /// <summary>
        /// Sets a gauge to specified value
        /// </summary>
        public void SetGauge(string eventName, double value)
        {
            if (string.IsNullOrWhiteSpace(eventName))
                throw new ArgumentNullException("Event name cannot be null");

            //DogStatsd.Gauge(eventName, value);
            _Connection.Send<Statsd.Gauge, double>("stat-name", 5.5);
        }

        /// <summary>
        /// Logs a time to the server in a dimensionless format.
        /// </summary>
        public void LogDuration(string eventName, double duration)
        {
            if (string.IsNullOrWhiteSpace(eventName))
                throw new ArgumentNullException("Event name cannot be null");

            _Connection.Send<Statsd.Timing, double>(eventName, duration);
        }

        /// <summary>
        /// Logs a time to the server in millisecond format.
        /// </summary>
        public void LogDurationInMs(string eventName, TimeSpan duration)
        {
            LogDuration(eventName, duration.TotalMilliseconds);
        }

        /// <summary>
        /// Logs a time to the server in millisecond format.
        /// </summary>
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

        /// <summary>
        /// Times a method and logs the value.
        /// </summary>
        public void LogDurationInMs(string eventName, Action method)
        {
            if (string.IsNullOrWhiteSpace(eventName))
                throw new ArgumentNullException("Event name cannot be null");

            _Connection.Send(method, eventName);
        }
    }
}
