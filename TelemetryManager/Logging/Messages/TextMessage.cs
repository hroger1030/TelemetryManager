namespace TelemetryManager
{
    internal class TextMessage : LogBase
    {
        public string Message { get; set; }

        public TextMessage(string message, string applicationName, string environmentName) : base(applicationName, environmentName)
        {
            Message = message;
        }

        public override string ToString()
        {
            return $"{Message}";
        }
    }
}
