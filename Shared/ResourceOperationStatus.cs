using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CogsMinimizer.Shared
{
    public class ResourceOperationResult
    {
        public ResourceOperationStatus Result { get; set; }

        public FailureReason FailureReason { get; set; }

        public string Message { get; set; }

        public ResourceOperationResult()
        {
            this.Result = ResourceOperationStatus.Succeeded;
            this.FailureReason = FailureReason.NoError;
        }
    }

    /// <summary>
    /// An enum that indicates the status of a resource in the cleanup lifecycle 
    /// </summary>
    public enum ResourceOperationStatus
    {
        Succeeded,
        Failed
    }

    public enum  FailureReason
    {
        NoError,
        WrongApiVersion,
        ResourceInUse,
        ResourceNotFound,
        ResourceTypeNotFound,
        UnknownError
    }
}
