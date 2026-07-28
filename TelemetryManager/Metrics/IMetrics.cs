using System;

namespace TelemetryManager
{
    public interface IMetrics
    {
        void DecrementCounter(string metricName, string[] tags = null);
        void IncrementCounter(string metricName, string[] tags = null);
        void IncrementCounter(string metricName, int count, string[] tags = null);
        void LogDurationInMs(string metricName, double duration, string[] tags = null);
        void LogDurationInMs(string metricName, DateTime start, DateTime end, string[] tags = null);
        void LogDurationInMs(string metricName, TimeSpan duration, string[] tags = null);
        void LogMethodDurationInMs(string metricName, Action method, string[] tags = null);
        T LogMethodDurationInMs<T>(string metricName, Func<T> method, string[] tags = null);
        void SetGauge(string metricName, double value, string[] tags = null);
        void Write(MetricType metricType, string metricName, string[] customTags, string value);
    }
}