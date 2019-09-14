namespace TelemetryManager
{
    public class TextMessage : LogBase
    {
        public string Message { get; set; }

        public TextMessage(string message)
        {
            Message = message;
        }

        public override string ToString()
        {
            return $"{Message}";
        }
    }
}
