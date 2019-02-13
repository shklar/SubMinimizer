namespace CogsMinimizer.Shared
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Configuration;

    /// <summary>
    /// A class that exposes methods that help create a tracer
    /// </summary>
    public static class TracerFactory
    {
        private static readonly string SessionId;

        private static readonly string InstrumentationKey;

        static TracerFactory()
        {
            SessionId = Guid.NewGuid().ToString();
            InstrumentationKey = ConfigurationManager.AppSettings["env:TelemetryInstrumentationKey"];
        }

        /// <summary>
        /// Create an instance of the tracer
        /// </summary>
        /// <param name="logger">Optional text writer logger (to be used in Web Job traces)</param>
        /// <returns>An object of <see cref="ITracer "/></returns>
        public static ITracer CreateTracer(TextWriter logger = null)
        {
            // Creates the aggregated tracer
            return new AggregatedTracer(GetTracersList(logger));
        }

        /// <summary>
        /// Get a list of <see cref="ITracer "/> objects for creating an aggregated tracer
        /// </summary>
        /// <param name="logger">Optional text writer logger (to be used in Web Job traces)</param>
        /// <returns>A list of <see cref="ITracer "/></returns>
        private static List<ITracer> GetTracersList(TextWriter logger = null)
        {
            List<ITracer> tracers = new List<ITracer>
            {
                new AITracer(SessionId, InstrumentationKey)
            };


            if (logger != null)
            {
                tracers.Add(new WebJobTracer(logger));
            }

            return tracers;
        }
    }
}
