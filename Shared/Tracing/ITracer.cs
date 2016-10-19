namespace CogsMinimizer.Shared
{
    /// <summary>
    /// Interface providing tracing capabilities
    /// </summary>
    public interface ITracer
    {
        /// <summary>
        /// Trace <paramref name="message"/> as Information message.
        /// </summary>
        /// <param name="message">The message to trace</param>
        void TraceInformation(string message);

        /// <summary>
        /// Trace <paramref name="message"/> as Error message.
        /// </summary>
        /// <param name="message">The message to trace</param>
        void TraceError(string message);

        /// <summary>
        /// Trace <paramref name="message"/> as Verbose message.
        /// </summary>
        /// <param name="message">The message to trace</param>
        void TraceVerbose(string message);

        /// <summary>
        /// Trace <paramref name="message"/> as Warning message.
        /// </summary>
        /// <param name="message">The message to trace</param>
        void TraceWarning(string message);

        /// <summary>
        /// Flushes the telemetry channel
        /// </summary>
        void Flush();
    }
}
