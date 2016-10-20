
namespace CogsMinimizer.Shared
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Implementation of the <see cref="ITracer"/> interface that traces to a (WebJob's) <see cref="TextWriter"/> logger.
    /// </summary>
    public class WebJobTracer : ITracer
    {
        private readonly TextWriter _logger;

        /// <summary>
        /// Initialized a new instance of the <see cref="WebJobTracer"/> class.
        /// </summary>
        /// <param name="logger">The logger to send traces to</param>
        public WebJobTracer(TextWriter logger) 
        {
            Diagnostics.EnsureArgumentNotNull( () => logger);

            // we keep a synchronized instance since logging can occur from multiple threads
            _logger = TextWriter.Synchronized(logger);
        }

        public void TraceInformation(string message)
        {
            _logger.WriteLine(message);
        }

        public void TraceError(string message)
        {
            _logger.WriteLine($"Error: {message}");
        }

        public void TraceVerbose(string message)
        {
            _logger.WriteLine($"Verbose: {message}");
        }

        public void TraceWarning(string message)
        {
            _logger.WriteLine($"Warning: {message}");
        }

        public void Flush()
        {
            _logger.Flush();
        }
    }
}
