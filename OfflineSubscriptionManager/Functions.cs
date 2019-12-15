using System;
using System.Configuration;
using System.IO;
using Microsoft.Azure.WebJobs;
using CogsMinimizer.Shared;

namespace OfflineSubscriptionManager
{
    public class Functions
    {
        // This function will be triggered based on the schedule you have set for this WebJob
        [NoAutomaticTrigger]
        public static void ManualTrigger(TextWriter logger)
        {
            Diagnostics.EnsureArgumentNotNull(() => logger);

            ITracer tracer = TracerFactory.CreateTracer(logger);
            tracer.TraceInformation("OfflineSubscriptionManager web job started!");
            tracer.TraceInformation($"Ikey: {ConfigurationManager.AppSettings["APPINSIGHTS_INSTRUMENTATIONKEY"]}");
            tracer.TraceInformation($"Application Key: {ConfigurationManager.AppSettings["ida:ClientID"]}");

            //Top level feature switch to allow easy disabling
            if (ConfigurationManager.AppSettings["env:EnableWebJob"].Equals("True", StringComparison.OrdinalIgnoreCase))
            {
                tracer.TraceInformation("EnableWebJob switch is Enabled.");
                SubscriptionProcessor.ProcessSubscriptions(tracer);
            }
            else
            {
                tracer.TraceWarning("EnableWebJob switch is disabled. Exiting.");
            }

            tracer.Flush();
        }

       
     


    
    }
}
