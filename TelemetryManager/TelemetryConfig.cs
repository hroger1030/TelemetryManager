using System;

namespace TelemetryManager
{
    public class TelemetryConfig
    {
        /// <summary>
        /// This method exists to give .net a reason to include the loggly assembly in the 
        /// telemetry manager output that is included in client applications.
        /// </summary>
        public void DummyCall()
        {
            new log4net.loggly.LogglyAppender();
            throw new Exception("Do not ever call DummyCall(), it exists for build purposes only");
        }
    }
}
