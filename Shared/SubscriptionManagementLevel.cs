using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CogsMinimizer.Shared
{
    /// <summary>
    /// Represents the category of operations that subminimizer is requested to apply to a subscription
    /// For instance, should expired resources be auto deleted or not 
    /// </summary>
    public enum SubscriptionManagementLevel
    {
        /// <summary>
        /// Subminimizer will only report about expired resources, but will never delete them.
        /// In this management level Subminimizer requires only write permission to the subscription.
        /// </summary>
        ReportOnly = 0
    }
}
