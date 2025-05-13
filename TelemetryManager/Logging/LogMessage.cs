namespace TelemetryManager
{
    public class LogMessage
    {
        public LoggingLevel Level { get; set; }
        public string Message { get; set; }
        public object Error { get; set; }
        public string ApplicationName { get; set; }
        public string Environment { get; set; }
    }
}
