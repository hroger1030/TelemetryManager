using System;

namespace TelemetryManager
{
    public class ScheduledTaskMessage : TextMessage
    {
        public string ClassName { get; set; }

        public static ScheduledTaskMessage Create<T>(string message)
        {
            return new ScheduledTaskMessage(message, typeof(T).Name);
        }

        public ScheduledTaskMessage(string message, string className) : base(message)
        {
            ClassName = className;
            Message = message;
        }

        public override string ToString()
        {
            return $"[{ClassName}]: {Message}";
        }
    }
}