using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using log4net.Appender;
using log4net.Core;
using Newtonsoft.Json;

namespace TelemetryManager
{
    /// <summary>
    /// Sends log4net messages to a listening REST api endpoint.
    /// </summary>
    internal class AsyncHttpAppender : AppenderSkeleton
    {
        private const string BACKGROUND_THREAD_NAME = "AsyncHttpAppender";
        private const int QUEUE_THREAD_SLEEP_DURATION_IN_MS = 1000 * 2;
        private const int HTTP_REQUEST_TIMEOUT_IN_MS = 1000 * 3;
        private const int MAX_QUEUE_LENGTH = 1000;

        private static HttpClient _HttpClient;
        private readonly ConcurrentQueue<LoggingEvent> _Queue;
        private bool _DoWork;
        private Thread _WorkerThread;

        public string UrlEndpoint { get; set; }

        public AsyncHttpAppender()
        {
            _HttpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMilliseconds(HTTP_REQUEST_TIMEOUT_IN_MS)
            };

            _Queue = new ConcurrentQueue<LoggingEvent>();
            _DoWork = true;

            _WorkerThread = new Thread(ProcessQueue)
            {
                Name = BACKGROUND_THREAD_NAME,
                IsBackground = true
            };
            _WorkerThread.Start();
        }

        protected override void Append(LoggingEvent[] loggingEvents)
        {
            foreach (var loggingEvent in loggingEvents)
                Append(loggingEvent);
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            if (!FilterEvent(loggingEvent))
                return;

            // if we are beyond the defined max queue size, we are 
            // want to drop the request to prevent performance issues.
            if (_Queue.Count > MAX_QUEUE_LENGTH)
                return;

            _Queue.Enqueue(loggingEvent);
        }

        protected override void OnClose()
        {
            _DoWork = false;
            _WorkerThread.Join();
            _WorkerThread = null;

            base.OnClose();
        }

        private void ProcessQueue()
        {
            while (_DoWork)
            {
                try
                {
                    if (_Queue.TryDequeue(out LoggingEvent loggingEvent))
                    {
                        Debug.WriteLine($"Dequeued message dispatched from {loggingEvent.LoggerName}");

                        // fire and forget task
                        Task.Factory.StartNew(() => Post(UrlEndpoint, loggingEvent));
                    }
                    else
                    {
                        if (_Queue.Count < 1)
                        {
                            Debug.WriteLine($"Sleeping for {QUEUE_THREAD_SLEEP_DURATION_IN_MS} ms");
                            Thread.Sleep(QUEUE_THREAD_SLEEP_DURATION_IN_MS);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"AsyncHttpAppender::processQueue() error: {ex}");
                }
            }
        }

        private async Task Post(string url, object postData)
        {
            try
            {
                using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    string buffer = JsonConvert.SerializeObject(postData);

                    requestMessage.Content = new StringContent(buffer, Encoding.UTF8, "application/json");
                    //requestMessage.Headers.Authorization = new AuthenticationHeaderValue("basic ", _ApiToken);

                    var response = await _HttpClient.SendAsync(requestMessage);

                    if (!response.IsSuccessStatusCode)
                        Debug.WriteLine($"Post request failure: {response.StatusCode} {response.RequestMessage}, Post: {buffer}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AsyncHttpAppender::Post() error: {ex}");
            }
        }
    }
}