using System;

namespace TelemetryManager
{
    internal class ErrorMessage : TextMessage
    {
        public Exception Exception { get; set; }

        public ErrorMessage(string message, Exception ex, string applicationName, string environmentName) : base(message, applicationName, environmentName)
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
