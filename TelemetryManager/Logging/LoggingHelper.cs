using System;

namespace TelemetryManager
{
    public static class LoggingHelper
    {
        public static LoggingLevel LoggingLevelStringMapper(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentNullException(nameof(input));

            switch (input.ToLower().Trim())
            {
                case "error": return LoggingLevel.Error;
                case "warn": return LoggingLevel.Warn;
                case "info": return LoggingLevel.Info;
                case "debug": return LoggingLevel.Debug;
                case "fatal": return LoggingLevel.Fatal;

                default: throw new ArgumentException($"Unable to parse '{input}' into a known LoggingLevel");
            }
        }
    }
}