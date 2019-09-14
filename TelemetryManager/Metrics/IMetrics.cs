using System;

namespace TelemetryManager
{
    public interface IMetrics : IDisposable
    {
        void IncrementCounter(string eventName, string[] tags = null);
        void DecrementCounter(string eventName, string[] tags = null);
        void IncrementCounter(string eventName, int count, string[] tags = null);

        void LogDuration(string eventName, double duration, string[] tags = null);
        void LogDurationInMs(string eventName, Action method, string[] tags = null);
        void LogDurationInMs(string eventName, TimeSpan duration, string[] tags = null);
        void LogDurationInMs(string eventName, DateTime start, DateTime end, string[] tags = null);

        void SetGauge(string eventName, double value, string[] tags = null);
    }
}