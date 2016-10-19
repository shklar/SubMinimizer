

namespace CogsMinimizer.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Implementation of the <see cref="ITracer"/> interface that traces to other <see cref="ITracer"/> objects.
    /// </summary>
    public partial class AggregatedTracer : ITracer
    {
        private readonly List<ITracer> _tracers;

        /// <summary>
        /// Initialized a new instance of the <see cref="AggregatedTracer"/> class.
        /// </summary>
        /// <param name="tracers">List of tracers to trace to</param>
        public AggregatedTracer(List<ITracer> tracers)
        {
            Diagnostics.EnsureArgumentNotNull(() => tracers);
            _tracers = new List<ITracer>(tracers.Where(t => t != null));

            Diagnostics.EnsureArgument(_tracers.Count > 0, () => tracers, "Must get at least one non-null tracer");
        }


        #region Implementation of ITracer

        /// <summary>
        /// Trace <paramref name="message"/> as Information
        /// </summary>
        /// <param name="message">The message to trace</param>
        public void TraceInformation(string message)
        {
            _tracers.ForEach(t => t.TraceInformation(message));
        }

        /// <summary>
        /// Trace <paramref name="message"/> as Error
        /// </summary>
        /// <param name="message">The message to trace</param>
        public void TraceError(string message)
        {
            _tracers.ForEach(t => t.TraceError(message));
        }

        /// <summary>
        /// Trace <paramref name="message"/> as Verbose.
        /// </summary>
        /// <param name="message">The message to trace</param>
        public void TraceVerbose(string message)
        {
            _tracers.ForEach(t => t.TraceVerbose(message));
        }

        /// <summary>
        /// Trace <paramref name="message"/> as Warning.
        /// </summary>
        /// <param name="message">The message to trace</param>
        public void TraceWarning(string message)
        {
            _tracers.ForEach(t => t.TraceWarning(message));
        }

        /// <summary>
        /// Flushes the telemetry channel
        /// </summary>
        public void Flush()
        {
            _tracers.ForEach(t => t.Flush());
        }

        #endregion
    }
}
