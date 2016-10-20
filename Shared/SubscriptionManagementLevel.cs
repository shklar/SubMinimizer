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
        /// In this management level Subminimizer requires only right permission to the subscription.
        /// </summary>
        ReportOnly = 0,

        /// <summary>
        /// Subminimizer will report and allow manually marking a resource for offline deletion.
        /// In this management level Subminimizer requires write permission to the subscription.
        ///  </summary>
        ManualDelete = 1,

        /// <summary>
        /// Subminimizer will report and automatically delete any expired resources.
        /// In this management level Subminimizer requires write permission to the subscription.
        ///  </summary>
        AutomaticDelete = 2
    }
}
