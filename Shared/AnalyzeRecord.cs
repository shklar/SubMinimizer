using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CogsMinimizer.Shared
{
    public class AnalyzeRecord  
    {
        /// <summary>
        /// Record ID
        /// </summary>
        [Key]
        public string ID { get; set; }

        /// <summary>
        /// Analyze date 
        /// </summary>
        public DateTime AnalyzeDate { get; set; }

        /// <summary>
        ///  Subscription name
        /// </summary>
        public string SubscriptionName { get; set; }

        /// <summary>
        ///  Subscription ID
        /// </summary>
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Owner
        /// </summary>
        public string Owner { get; set; }


        /// <summary>
        /// Deleted resources
        /// </summary>
        public int DeletedResources { get; set; }

        /// <summary>
        /// Not found resources
        /// </summary>
        public int NotFoundResources { get; set; }

        /// <summary>
        ///  Failed to delete resources
        /// </summary>
        public int FailedDeleteResources { get; set; }
 
        /// <summary>
        /// Total resources
        /// </summary>
        public int TotalResources { get; set; }

        /// <summary>
        ///  Expired resources
        /// </summary>
        public int ExpiredResources { get; set; }

        /// <summary>
        /// Valid resources
        /// </summary>
        public int ValidResources { get; set; }

        /// <summary>
        ///  New resources
        /// </summary>
        public int NewResources { get; set; }

        /// <summary>
        ///  Near to expired resources
        /// </summary>
        public int NearExpiredResources { get; set; }
    }
}