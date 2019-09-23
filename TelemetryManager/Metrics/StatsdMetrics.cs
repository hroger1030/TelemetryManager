using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace TelemetryManager
{
    /// <summary>
    /// Sends statsd style metrics to datadog via API to avoid having to install agents on host machines.
    /// </summary>
    public class StatsdMetrics : IMetrics
    {
        private const string SECTION_DELIMITER = "+++++++++++++++++++++++++++";
        private const int HTTP_REQUEST_TIMEOUT_IN_MS = 1000 * 3;

        private static HttpClient _HttpClient;
        private string[] _MetricTagCache;
        private string _FullUrlCache;

        private readonly string _ApplicationName;
        private readonly string _Environment;
        private readonly string _UrlEndpoint;
        private readonly string _ApiKey;
        private bool _IsDisposed;

        /// <summary>
        /// CTOR for class that takes in a configuration collection
        /// </summary>
        public StatsdMetrics(IDictionary<string, string> config) : this(config[TelemetryConfig.API_URL], config[TelemetryConfig.API_KEY], config[TelemetryConfig.APP_NAME], config[TelemetryConfig.ENVIRONMENT]) { }

        /// <summary>
        /// CTOR for class
        /// </summary>
        /// <param name="urlEndpoint">full url of api endpoint, sans query string parameters</param>
        /// <param name="apiKey">api key for hosted account</param>
        /// <param name="applicationName">A string that denotes the name of the application that you are working in.</param>
        /// <param name="environment">A string denoting the environment that you are working in. Typically this will be prod|stage|dev.</param>
        public StatsdMetrics(string urlEndpoint, string apiKey, string applicationName, string environment)
        {
            if (string.IsNullOrWhiteSpace(urlEndpoint))
                throw new ArgumentNullException("Url endpoint name cannot be null or empty");

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentNullException("Api key name cannot be null or empty");

            if (string.IsNullOrWhiteSpace(applicationName))
                throw new ArgumentNullException("Application name cannot be null or empty");

            if (string.IsNullOrWhiteSpace(environment))
                throw new ArgumentNullException("Environment cannot be null or empty");

            _HttpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMilliseconds(HTTP_REQUEST_TIMEOUT_IN_MS)
            };

            _ApplicationName = applicationName.Replace(" ", string.Empty).ToLower();
            _Environment = environment.Replace(" ", string.Empty).ToLower();
            _UrlEndpoint = urlEndpoint;
            _ApiKey = apiKey;
            _IsDisposed = false;
        }

        public void IncrementCounter(string metricName, string[] tags = null)
        {
            IncrementCounter(metricName, 1, tags);
        }

        public void DecrementCounter(string metricName, string[] tags = null)
        {
            IncrementCounter(metricName, -1, tags);
        }

        public void IncrementCounter(string metricName, int count, string[] tags = null)
        {
            SendMetric(MetricType.count, metricName, tags, count.ToString());
        }

        /// <summary>
        /// Sets a gauge to specified value
        /// </summary>
        public void SetGauge(string metricName, double value, string[] tags = null)
        {
            SendMetric(MetricType.gague, metricName, tags, value.ToString());
        }

        /// <summary>
        /// Logs a time to the server in a dimensionless format.
        /// </summary>
        public void LogDuration(string metricName, double duration, string[] tags = null)
        {
            SendMetric(MetricType.rate, metricName, tags, duration.ToString());
        }

        /// <summary>
        /// Logs a time to the server in millisecond format.
        /// </summary>
        public void LogDurationInMs(string metricName, TimeSpan duration, string[] tags = null)
        {
            SendMetric(MetricType.rate, metricName, tags, duration.TotalMilliseconds.ToString());
        }

        /// <summary>
        /// Logs a time to the server in millisecond format.
        /// </summary>
        public void LogDurationInMs(string metricName, DateTime start, DateTime end, string[] tags = null)
        {
            if (end < start)
            {
                var buffer = end;
                end = start;
                start = buffer;
            }

            SendMetric(MetricType.rate, metricName, tags, (end - start).TotalMilliseconds.ToString());
        }

        /// <summary>
        /// Times a method and logs the value.
        /// </summary>
        public void LogDurationInMs(string metricName, Action method, string[] tags = null)
        {
            var sw = Stopwatch.StartNew();
            method();
            sw.Stop();

            SendMetric(MetricType.rate, metricName, tags, sw.ElapsedMilliseconds.ToString());
        }

        private void SendMetric(MetricType metricType, string metricName, string[] customTags, string value)
        {
            // Tags are used by datadog to allow metric aggregation across logical groups. (https://docs.datadoghq.com/tagging/)
            if (_MetricTagCache == null)
                _MetricTagCache = new[] { $"env:{_Environment}", $"source:{_ApplicationName}" };

            var tagsList = new List<string>();
            tagsList.AddRange(_MetricTagCache);

            if (customTags != null && customTags.Length > 0)
                tagsList.AddRange(customTags);

            var tags = tagsList.ToArray();

            var buffer = new MetricsPayload()
            {
                Series = new MetricData[]
                {
                    new MetricData()
                    {
                        Host = Environment.MachineName,
                        MetricName = metricName,
                        Tags = tags,
                        MetricType = metricType.ToString(),
                        DataPoints = new string[][]
                        {
                            new string[]
                            {
                                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                                value
                            }
                        }
                    }
                }
            };

            if (_FullUrlCache == null)
                _FullUrlCache = $"{_UrlEndpoint}?api_key={_ApiKey}";

            // fire and forget task
            Task.Factory.StartNew(() => Post(_FullUrlCache, buffer));
        }

        private async Task Post(string url, object postData)
        {
            try
            {
                using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    string buffer = JsonConvert.SerializeObject(postData);
                    requestMessage.Content = new StringContent(buffer, Encoding.UTF8, "application/json");

                    var response = await _HttpClient.SendAsync(requestMessage);

                    if (!response.IsSuccessStatusCode)
                    {
                        Debug.WriteLine(SECTION_DELIMITER);
                        Debug.WriteLine("Http request error");
                        Debug.WriteLine($"Url: {url}");
                        Debug.WriteLine($"Status code: {response.StatusCode}");
                        Debug.WriteLine($"Response: {response.RequestMessage}");
                        Debug.WriteLine($"Request body: {buffer}");
                        Debug.WriteLine(SECTION_DELIMITER);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AsyncHttpAppender::Post() error: {ex}");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_IsDisposed)
            {
                if (disposing)
                {
                    // dispose managed objects
                    if (_HttpClient != null)
                    {
                        _HttpClient.Dispose();
                        _HttpClient = null;
                    }
                }

                _IsDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #region API call classes

        private class MetricsPayload
        {
            [JsonProperty(PropertyName = "series")]
            public MetricData[] Series { get; set; }
        }

        private class MetricData
        {
            [JsonProperty(PropertyName = "host")]
            public string Host { get; set; }

            [JsonProperty(PropertyName = "metric")]
            public string MetricName { get; set; }

            [JsonProperty(PropertyName = "points")]
            public string[][] DataPoints { get; set; }

            [JsonProperty(PropertyName = "tags")]
            public string[] Tags { get; set; }

            [JsonProperty(PropertyName = "type")]
            public string MetricType { get; set; }
        }

        /// <summary>
        /// metric types supported by datadog
        /// </summary>
        private enum MetricType
        {
            gague,
            rate,
            count
        }

        #endregion
    }
}