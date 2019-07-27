using System;

namespace TelemetryManager
{
    public interface IMetrics : IDisposable
    {
        void IncrementCounter(string eventName);
        void DecrementCounter(string eventName);
        void IncrementCounter(string eventName, int count);

        void LogDuration(string eventName, double duration);
        void LogDurationInMs(string eventName, Action method);
        void LogDurationInMs(string eventName, TimeSpan duration);
        void LogDurationInMs(string eventName, DateTime start, DateTime end);

        void SetGauge(string eventName, double value);
    }
}