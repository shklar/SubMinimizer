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
        public SubscriptionAnalysisResult(Subscription sub)
        {
            this.AnalyzedSubscription = sub;
            ExpiredResources = new List<Resource>();
            DeletedResources = new List<Resource>();
            FailedDeleteResources = new List<Resource>();
            NotFoundResources = new List<Resource>();
            NewResources = new List<Resource>();
        }

        /// <summary>
        /// The subscription that was analyzed
        /// </summary>
        public Subscription AnalyzedSubscription { get; private set; }
 
        /// <summary>
        /// Indicates whether the application had permissions to analyze the subscription
        /// </summary>
        public bool IsSubscriptionAccessible { get; set; }

        /// <summary>
        /// A list of all the expired resources found within the subscription
        /// </summary>
        public List<Resource> ExpiredResources { get; set; }

        /// <summary>
        /// A list of all the near expiration resources found within the subscription
        /// </summary>
        public List<Resource> NearExpiredResources { get; set; }

        /// <summary>
        /// A list of all the resources that were deleted during the operation
        /// </summary>
        public List<Resource> DeletedResources { get; set; }

        /// <summary>
        /// A list of all the resources that wfailed deleting during the operation
        /// </summary>
        public List<Resource> FailedDeleteResources { get; set; }

        /// <summary>
        /// A list of resources that were previously tracked but are no longer found in the subscription
        /// </summary>
        public List<Resource> NotFoundResources { get; set; }

        /// <summary>
        /// A list of resources found for the first time
        /// </summary>
        public List<Resource> NewResources { get; set; }

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