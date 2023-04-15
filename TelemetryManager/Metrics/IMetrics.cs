using System;
using System.Threading.Tasks;

namespace TelemetryManager
{
    public interface IMetrics
    {
        Task DecrementCounter(string metricName, string[] tags = null);
        Task IncrementCounter(string metricName, string[] tags = null);
        Task IncrementCounter(string metricName, int count, string[] tags = null);
        Task LogDurationInMs(string metricName, double duration, string[] tags = null);
        Task LogDurationInMs(string metricName, DateTime start, DateTime end, string[] tags = null);
        Task LogDurationInMs(string metricName, TimeSpan duration, string[] tags = null);
        Task LogMethodDurationInMs(string metricName, Action method, string[] tags = null);
        Task<T> LogMethodDurationInMs<T>(string metricName, Func<T> method, string[] tags = null);
        Task SetGauge(string metricName, double value, string[] tags = null);
    }
}