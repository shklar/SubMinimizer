namespace CogsMinimizer.Shared
{
    using System;
    using System.Collections.Generic;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;

    /// <summary>
    /// Implementation of the <see cref="ITracer"/> interface that traces to AppInsights.
    /// </summary>
    public class AITracer : ITracer
    {
        private readonly TelemetryClient _telemetryClient;
        /// <summary>
        /// 
        /// The maximum length of an exception message allowed by AI SDK. 
        /// </summary>
        public const int ExceptionMessageMaxLength = 1024;

        /// <summary>
        /// Initialized a new instance of the <see cref="AITracer"/> class.
        /// </summary>
        /// <param name="sessionId">Session id used for tracing</param>
        /// <param name="instrumentationKey">the instrumentation key</param>
        public AITracer(string sessionId, string instrumentationKey)
        {
            TelemetryConfiguration.Active.InstrumentationKey = instrumentationKey;
            _telemetryClient = new TelemetryClient();
            _telemetryClient.Context.Session.Id = sessionId;
        }

        /// <summary>
        /// Trace <paramref name="message"/> as <see cref="SeverityLevel.Information"/> message.
        /// </summary>
        /// <param name="message">The message to trace</param>
        public virtual void TraceInformation(string message)
        {
            this.Trace(message, SeverityLevel.Information);
        }

        /// <summary>
        /// Trace <paramref name="message"/> as <see cref="SeverityLevel.Error"/> message.
        /// </summary>
        /// <param name="message">The message to trace</param>
        public virtual void TraceError(string message)
        {
            this.Trace(message, SeverityLevel.Error);
        }

        /// <summary>
        /// Trace <paramref name="message"/> as <see cref="SeverityLevel.Verbose"/> message.
        /// </summary>
        /// <param name="message">The message to trace</param>
        public virtual void TraceVerbose(string message)
        {
            this.Trace(message, SeverityLevel.Verbose);
        }

        /// <summary>
        /// Trace <paramref name="message"/> as <see cref="SeverityLevel.Warning"/> message.
        /// </summary>
        /// <param name="message">The message to trace</param>
        public virtual void TraceWarning(string message)
        {
            this.Trace(message, SeverityLevel.Warning);
        }


        /// <summary>
        /// Flushes the telemetry channel
        /// </summary>
        public void Flush()
        {
            _telemetryClient.Flush();
        }

        #region Private helper methods

        /// <summary>
        /// Traces the specified message to the telemetry client
        /// </summary>
        /// <param name="message">The message to trace</param>
        /// <param name="severityLevel">The message's severity level</param>
        private void Trace(string message, SeverityLevel severityLevel)
        {
            var traceTelemetry = new TraceTelemetry(message, severityLevel);
            _telemetryClient.TrackTrace(traceTelemetry);
        }

        #endregion

    }
}
