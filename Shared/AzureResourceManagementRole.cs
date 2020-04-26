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
    public enum AzureResourceManagementRole
    {
        Reader = 0
    }
}
