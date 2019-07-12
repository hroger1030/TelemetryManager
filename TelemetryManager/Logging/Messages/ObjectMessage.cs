namespace TelemetryManager
{
    internal class ObjectMessage : TextMessage
    {
        public object Data { get; set; }

        public ObjectMessage(string message, object data, string applicationName, string environmentName) : base(message, applicationName, environmentName)
        {
            Data = data;
        }

        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(Message))
                return $"{Data}";
            else
                return $"{Message} {Data}";
        }
    }
}
