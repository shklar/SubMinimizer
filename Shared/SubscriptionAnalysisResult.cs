using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity.Migrations.Model;

namespace CogsMinimizer.Shared
{
    /// <summary>
    /// Represents the outcome of analyzing a subscription
    /// </summary>
    public class SubscriptionAnalysisResult
    {
        /// <summary>
        /// Ctor
        /// </summary>
        public SubscriptionAnalysisResult()
        {
                ExpiredResources = new List<Resource>();
        }

        /// <summary>
        /// The subscription that was analyzed
        /// </summary>
        public Subscription AnalyzedSubscription { get; set; }
        /// <summary>
        /// Indicates whether the application had permissions to analyze the subscription
        /// </summary>
        public bool IsSubscriptionAccessible { get; set; }

        /// <summary>
        /// A list of all the expired resources found within the subscription
        /// </summary>
        public List<Resource> ExpiredResources { get; set; }

        /// <summary>
        /// The time at which the analysis started
        /// </summary>
        public DateTime AnalysisStartTime { get; set; }

        /// <summary>
        /// The time at which the analysis ended
        /// </summary>
        public DateTime AnalysisEndTime { get; set; }


    }
}