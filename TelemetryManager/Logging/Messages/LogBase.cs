namespace TelemetryManager
{
    public abstract class LogBase
    {
        public string ApplicationName { get; set; }
        public string Environment { get; set; }

        public LogBase() { }
    }
}
