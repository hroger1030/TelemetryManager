using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TelemetryManager
{
    /// <summary>
    /// Sends statsd style metrics to datadog via API to avoid having to install agents on host machines.
    /// </summary>
    public class StatsdMetrics : IMetrics, IDisposable
    {
        private const int HTTP_REQUEST_TIMEOUT_IN_MS = 1000 * 3;

        private static HttpClient _HttpClient;
        private readonly string[] _MetricTagCache;
        private readonly string _ApplicationName;
        private readonly string _Environment;
        private readonly string _UrlEndpoint;
        private readonly string _ApiKey;
        private bool _IsDisposed;

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
                throw new ArgumentNullException(nameof(urlEndpoint));

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentNullException(nameof(apiKey));

            if (string.IsNullOrWhiteSpace(applicationName))
                throw new ArgumentNullException(nameof(applicationName));

            if (string.IsNullOrWhiteSpace(environment))
                throw new ArgumentNullException(nameof(environment));

            _HttpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMilliseconds(HTTP_REQUEST_TIMEOUT_IN_MS)
            };

            _ApplicationName = applicationName.Replace(" ", string.Empty).ToLower();
            _Environment = environment.Replace(" ", string.Empty).ToLower();
            _UrlEndpoint = $"{_UrlEndpoint}?api_key={_ApiKey}";
            _MetricTagCache = new[] { $"env:{_Environment}", $"source:{_ApplicationName}" };
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
            Write(MetricType.count, metricName, tags, count.ToString());
        }

        /// <summary>
        /// Sets a gauge to specified value
        /// </summary>
        public void SetGauge(string metricName, double value, string[] tags = null)
        {
            Write(MetricType.gauge, metricName, tags, value.ToString());
        }

        /// <summary>
        /// Logs a time to the server in a dimensionless format.
        /// </summary>
        public void LogDurationInMs(string metricName, double duration, string[] tags = null)
        {
            Write(MetricType.rate, metricName, tags, duration.ToString());
        }

        /// <summary>
        /// Logs a time to the server in millisecond format.
        /// </summary>
        public void LogDurationInMs(string metricName, TimeSpan duration, string[] tags = null)
        {
            Write(MetricType.rate, metricName, tags, duration.TotalMilliseconds.ToString());
        }

        /// <summary>
        /// Logs a time to the server in millisecond format.
        /// </summary>
        public void LogDurationInMs(string metricName, DateTime start, DateTime end, string[] tags = null)
        {
            if (end < start)
                (start, end) = (end, start);

            Write(MetricType.rate, metricName, tags, (end - start).TotalMilliseconds.ToString());
        }

        public T LogMethodDurationInMs<T>(string metricName, Func<T> method, string[] tags = null)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                return method();
            }
            finally
            {
                sw.Stop();
                Write(MetricType.rate, metricName, tags, sw.ElapsedMilliseconds.ToString());
            }
        }

        public void LogMethodDurationInMs(string metricName, Action method, string[] tags = null)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                method();
            }
            finally
            {
                sw.Stop();
                Write(MetricType.rate, metricName, tags, sw.ElapsedMilliseconds.ToString());
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

            var buffer = new
            {
                series = new object[]
                {
                    new
                    {
                        host = Environment.MachineName,
                        metric = metricName,
                        tags = formattedTags,
                        type = metricType.ToString(),
                        points = new string[][]
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

            // fire and forget task
            Post(_UrlEndpoint, buffer);
        }

        private static void Post(string url, object postData)
        {
            try
            {
                string buffer = JsonConvert.SerializeObject(postData);

                using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(buffer, Encoding.UTF8, "application/json"),
                };

                var response = _HttpClient.Send(requestMessage);

                if (!response.IsSuccessStatusCode)
                {
                    string SECTION_DELIMITER = "+++++++++++++++++++++++++++";

                    Debug.WriteLine(SECTION_DELIMITER);
                    Debug.WriteLine("Http request error");
                    Debug.WriteLine($"Url: {url}");
                    Debug.WriteLine($"Status code: {response.StatusCode}");
                    Debug.WriteLine($"Response: {response.RequestMessage}");
                    Debug.WriteLine($"Request body: {buffer}");
                    Debug.WriteLine(SECTION_DELIMITER);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AsyncHttpAppender::Post() error: {ex}");
            }
        }

        private void Dispose(bool disposing)
        {
            if (_IsDisposed)
                return;

            if (disposing)
            {
                if (_HttpClient != null)
                {
                    _HttpClient.Dispose();
                    _HttpClient = null;
                }
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
            await IncrementCounterAsync(metricName, 1, tags);
        }

        public async Task DecrementCounterAsync(string metricName, string[] tags = null)
        {
            await IncrementCounterAsync(metricName, -1, tags);
        }

        public async Task IncrementCounterAsync(string metricName, int count, string[] tags = null)
        {
            await WriteAsync(MetricType.count, metricName, tags, count.ToString());
        }

        /// <summary>
        /// Sets a gauge to specified value
        /// </summary>
        public async Task SetGaugeAsync(string metricName, double value, string[] tags = null)
        {
            await WriteAsync(MetricType.gauge, metricName, tags, value.ToString());
        }

        /// <summary>
        /// Logs a time to the server in a dimensionless format.
        /// </summary>
        public async Task LogDurationInMsAsync(string metricName, double duration, string[] tags = null)
        {
            await WriteAsync(MetricType.rate, metricName, tags, duration.ToString());
        }

        /// <summary>
        /// Logs a time to the server in millisecond format.
        /// </summary>
        public async Task LogDurationInMsAsync(string metricName, TimeSpan duration, string[] tags = null)
        {
            await WriteAsync(MetricType.rate, metricName, tags, duration.TotalMilliseconds.ToString());
        }

        /// <summary>
        /// Logs a time to the server in millisecond format.
        /// </summary>
        public async Task LogDurationInMsAsync(string metricName, DateTime start, DateTime end, string[] tags = null)
        {
            if (end < start)
                (start, end) = (end, start);

            await WriteAsync(MetricType.rate, metricName, tags, (end - start).TotalMilliseconds.ToString());
        }

        public async Task<T> LogMethodDurationInMsAsync<T>(string metricName, Func<T> method, string[] tags = null)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                return method();
            }
            finally
            {
                sw.Stop();
                await WriteAsync(MetricType.rate, metricName, tags, sw.ElapsedMilliseconds.ToString());
            }
        }

        public async Task LogMethodDurationInMsAsync(string metricName, Action method, string[] tags = null)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                method();
            }
            finally
            {
                sw.Stop();
                await WriteAsync(MetricType.rate, metricName, tags, sw.ElapsedMilliseconds.ToString());
            }
        }

        public async Task WriteAsync(MetricType metricType, string metricName, string[] customTags, string value)
        {
            await Task.Run(() => Write(metricType, metricName, customTags, value));
        }
    }
}