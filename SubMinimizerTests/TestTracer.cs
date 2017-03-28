namespace CogsMinimizer.Shared
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Implementation of the <see cref="ITracer"/> interface that traces to nowhere.
    /// </summary>
    public class TestTracer : ITracer
    {

        /// <summary>
        /// Initialized a new instance of the <see cref="AITracer"/> class.
        /// </summary>
        public TestTracer()
        {
        }

        /// <summary>
        /// Trace <paramref name="message"/> as <see cref="SeverityLevel.Information"/> message.
        /// </summary>
        /// <param name="message">The message to trace</param>
        public virtual void TraceInformation(string message)
        {
        }

        /// <summary>
        /// Trace <paramref name="message"/> as <see cref="SeverityLevel.Error"/> message.
        /// </summary>
        /// <param name="message">The message to trace</param>
        public virtual void TraceError(string message)
        {
        }

        /// <summary>
        /// Trace <paramref name="message"/> as <see cref="SeverityLevel.Verbose"/> message.
        /// </summary>
        /// <param name="message">The message to trace</param>
        public virtual void TraceVerbose(string message)
        {
        }

        /// <summary>
        /// Trace <paramref name="message"/> as <see cref="SeverityLevel.Warning"/> message.
        /// </summary>
        /// <param name="message">The message to trace</param>
        public virtual void TraceWarning(string message)
        {
        }


        /// <summary>
        /// Flushes the telemetry channel
        /// </summary>
        public void Flush()
        {
        }

        #region Private helper methods

        #endregion

    }
}
