using System;

namespace TelemetryManager
{
    public class ErrorMessage : TextMessage
    {
        public Exception Exception { get; set; }

        public ErrorMessage(string message, Exception ex) : base(message)
        {
            Exception = ex;
        }

        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(Message))
                return $"{Exception}";
            else
                return $"{Message} {Exception}";
        }
    }
}
