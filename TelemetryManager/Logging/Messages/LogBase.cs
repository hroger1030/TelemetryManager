namespace TelemetryManager
{
    internal abstract class LogBase
    {
        public string ApplicationName { get; set; }
        public string Environment { get; set; }

        public LogBase() { }

        public LogBase(string application, string environment)
        {
            ApplicationName = application;
            Environment = environment;
        }
    }
}
