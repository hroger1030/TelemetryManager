namespace TelemetryManager
{
    public class ObjectMessage : TextMessage
    {
        public object Data { get; set; }

        public ObjectMessage(string message, object data) : base(message)
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
