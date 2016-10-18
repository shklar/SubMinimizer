using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CogsMinimizer.Shared
{
    /// <summary>
    /// An enum that indicates the status of a resource in the cleanup lifecycle 
    /// </summary>
    public enum ResourceStatus
    {
        Valid,
        Expired,
        MarkedForDeletion,
        Reserved
    }
}
