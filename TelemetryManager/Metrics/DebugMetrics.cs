using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace TelemetryManager
{
    /// <summary>
    /// Sends metrics to debug console.
    /// </summary>
    public class DebugMetrics : IMetrics, IDisposable
    {
        private readonly string[] _MetricTagCache;
        private readonly string _ApplicationName;
        private readonly string _Environment;
        private bool _IsDisposed;

        public List<MetricData> RecordedMetrics { get; private set; } = new();

        public DebugMetrics(string applicationName, string environment)
        {
            if (string.IsNullOrWhiteSpace(applicationName))
                throw new ArgumentNullException(nameof(applicationName));

            if (string.IsNullOrWhiteSpace(environment))
                throw new ArgumentNullException(nameof(environment));

            _ApplicationName = applicationName.Replace(" ", string.Empty);
            _Environment = environment.Replace(" ", string.Empty);
            _MetricTagCache = new[] { $"env:{_Environment}", $"source:{_ApplicationName}" };
            _IsDisposed = false;
        }

        public async Task IncrementCounter(string eventName, string[] tags)
        {
            await IncrementCounter(eventName, 1, tags);
        }

        public async Task DecrementCounter(string eventName, string[] tags)
        {
            await IncrementCounter(eventName, -1, tags);
        }

        public async Task IncrementCounter(string metricName, int count, string[] tags = null)
        {
            await Write(MetricType.count, metricName, tags, DateTime.UtcNow, count.ToString());
        }

        public async Task SetGauge(string metricName, double value, string[] tags = null)
        {
            await Write(MetricType.gauge, metricName, tags, DateTime.UtcNow, value.ToString());
        }

        public async Task LogDurationInMs(string metricName, double durationInMs, string[] tags = null)
        {
            await Write(MetricType.rate, metricName, tags, DateTime.UtcNow, durationInMs.ToString());
        }

        public async Task LogDurationInMs(string metricName, TimeSpan duration, string[] tags = null)
        {
            await Write(MetricType.rate, metricName, tags, DateTime.UtcNow, duration.TotalMilliseconds.ToString());
        }

        public async Task LogDurationInMs(string metricName, DateTime start, DateTime end, string[] tags = null)
        {
            if (end < start)
                (start, end) = (end, start);

            await Write(MetricType.rate, metricName, tags, DateTime.UtcNow, (end - start).TotalMilliseconds.ToString());
        }

        public async Task LogMethodDurationInMs(string metricName, Action method, string[] tags = null)
        {
            var timer = Stopwatch.StartNew();

            try
            {
                method();
            }
            finally
            {
                timer.Stop();
                await Write(MetricType.rate, metricName, tags, DateTime.UtcNow, timer.ElapsedMilliseconds.ToString());
            }
        }

        public async Task<T> LogMethodDurationInMs<T>(string metricName, Func<T> method, string[] tags = null)
        {
            var timer = Stopwatch.StartNew();

            try
            {
                return method();
            }
            finally
            {
                timer.Stop();
                await Write(MetricType.rate, metricName, tags, DateTime.UtcNow, timer.ElapsedMilliseconds.ToString());
            }
        }

        private async Task Write(MetricType metricType, string metricName, string[] customTags, DateTime eventDate, string value)
        {
            if (string.IsNullOrWhiteSpace(metricName))
                throw new ArgumentNullException(nameof(metricName));

            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(nameof(value));

            var tagsList = new List<string>(_MetricTagCache);

            if (customTags != null && customTags.Length > 0)
                tagsList.AddRange(customTags);

            var formattedTags = tagsList.ToArray();

            var metric = new MetricData()
            {
                MachineName = Environment.MachineName,
                MetricName = metricName,
                FormattedTags = formattedTags,
                MetricType = metricType.ToString(),
                DataPoints = new string[][]
                {
                    new string[]
                    {
                        new DateTimeOffset(eventDate).ToUnixTimeSeconds().ToString(),
                        value
                    }
                }
            };

            RecordedMetrics.Add(metric);
            Debug.WriteLine($"(DebugMetricWriter): {metric}");

            await Task.CompletedTask;
        }

        private void Dispose(bool disposing)
        {
            if (!_IsDisposed)
            {
                if (disposing)
                {
                    RecordedMetrics = null;
                }

                _IsDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    public class MetricData
    {
        public string MachineName { get; set; }
        public string MetricName { get; set; }
        public string[] FormattedTags { get; set; }
        public string MetricType { get; set; }
        public string[][] DataPoints { get; set; }
    }
}
