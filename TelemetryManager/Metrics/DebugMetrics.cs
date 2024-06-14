using Newtonsoft.Json.Linq;
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


        public void IncrementCounter(string eventName, string[] tags)
        {
            IncrementCounter(eventName, 1, tags);
        }

        public void DecrementCounter(string eventName, string[] tags)
        {
            IncrementCounter(eventName, -1, tags);
        }

        public void IncrementCounter(string metricName, int count, string[] tags = null)
        {
            Write(MetricType.count, metricName, tags, count.ToString());
        }

        public void SetGauge(string metricName, double value, string[] tags = null)
        {
            Write(MetricType.gauge, metricName, tags, value.ToString());
        }

        public void LogDurationInMs(string metricName, double durationInMs, string[] tags = null)
        {
            Write(MetricType.rate, metricName, tags, durationInMs.ToString());
        }

        public void LogDurationInMs(string metricName, TimeSpan duration, string[] tags = null)
        {
            Write(MetricType.rate, metricName, tags, duration.TotalMilliseconds.ToString());
        }

        public void LogDurationInMs(string metricName, DateTime start, DateTime end, string[] tags = null)
        {
            if (end < start)
                (start, end) = (end, start);

            Write(MetricType.rate, metricName, tags, (end - start).TotalMilliseconds.ToString());
        }

        public void LogMethodDurationInMs(string metricName, Action method, string[] tags = null)
        {
            var timer = Stopwatch.StartNew();

            try
            {
                method();
            }
            finally
            {
                timer.Stop();
                Write(MetricType.rate, metricName, tags, timer.ElapsedMilliseconds.ToString());
            }
        }

        public T LogMethodDurationInMs<T>(string metricName, Func<T> method, string[] tags = null)
        {
            var timer = Stopwatch.StartNew();

            try
            {
                return method();
            }
            finally
            {
                timer.Stop();
                Write(MetricType.rate, metricName, tags, timer.ElapsedMilliseconds.ToString());
            }
        }

        public void Write(MetricType metricType, string metricName, string[] customTags, string value)
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
                        DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                        value
                    }
                }
            };

            RecordedMetrics.Add(metric);
            Debug.WriteLine($"(DebugMetricWriter): {metric}");
        }

        private void Dispose(bool disposing)
        {
            if (_IsDisposed)
                return;

            if (disposing)
            {
                RecordedMetrics = null;
            }

            _IsDisposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        public async Task IncrementCounterAsync(string metricName, string[] tags = null)
        {
            await Task.Run(() => IncrementCounter(metricName, tags));
        }

        public async Task DecrementCounterAsync(string eventName, string[] tags)
        {
            await Task.Run(() => DecrementCounter(eventName, tags));
        }

        public async Task IncrementCounterAsync(string metricName, int count, string[] tags = null)
        {
            await Task.Run(() => IncrementCounter(metricName, count, tags));
        }

        public async Task SetGaugeAsync(string metricName, double value, string[] tags = null)
        {
            await Task.Run(() => SetGauge(metricName, value, tags));
        }

        public async Task LogDurationInMsAsync(string metricName, double durationInMs, string[] tags = null)
        {
            await Task.Run(() => LogDurationInMs(metricName, durationInMs, tags));
        }

        public async Task LogDurationInMsAsync(string metricName, TimeSpan duration, string[] tags = null)
        {
            await Task.Run(() => LogDurationInMs(metricName, duration, tags));
        }

        public async Task LogDurationInMsAsync(string metricName, DateTime start, DateTime end, string[] tags = null)
        {
            await Task.Run(() => LogDurationInMs(metricName, start, end, tags));
        }

        public async Task LogMethodDurationInMsAsync(string metricName, Action method, string[] tags = null)
        {
            await Task.Run(() => LogMethodDurationInMs(metricName, method, tags));
        }

        public async Task<T> LogMethodDurationInMsAsync<T>(string metricName, Func<T> method, string[] tags = null)
        {
            return await Task.Run(() => LogMethodDurationInMs(metricName, method, tags));
        }

        public async Task WriteAsync(MetricType metricType, string metricName, string[] customTags, string value)
        {
            await Task.Run(() => Write(metricType, metricName, customTags, value));
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
