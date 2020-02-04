using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace CogsMinimizer.Shared
{
    [DataContract]
    public class Resource    
    {
        /// <summary>
        /// Resource ID
        /// </summary>

        
        public string Id { get; set; }

        /// <summary>
        /// Azure name of the resource
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// The Azure resource type (storage/compute/DB)
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The name of the resource owner 
        /// </summary>
        public string Owner { get; set; }

        /// <summary>
        /// Yes if the owner was explicitly confirmed by the user. False if inferred by the application
        /// </summary>
        public bool ConfirmedOwner { get; set; }
       
        /// <summary>
        /// The resource Group to which this resource belongs
        /// </summary>
        public string ResourceGroup { get; set; }

        /// <summary>
        ///  Resource description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The first date when the resource was encountered
        /// </summary>
        [DataType(DataType.Date)]
        [Display(Name = "FirstFound")]
        public DateTime FirstFoundDate { get; set; }

        /// <summary>
        /// The date on which this resource would expire
        /// </summary>
        [DataType(DataType.Date)]
        [Display(Name = "Expiration")]
        public DateTime ExpirationDate { get; set; }

        /// <summary>
        /// The last time the application analyzed this resource
        /// </summary>
        public DateTime LastVisitedDate { get; set; }
       
        /// <summary>
        /// The Azure Resource ID
        /// </summary>
        public string AzureResourceIdentifier { get; set; }

        /// <summary>
        /// Indicates whether this resource has outlived the configured threshold
        /// </summary>
        [NotMapped]
        public bool Expired { get; set; }

        /// <summary>
        /// The subscription id of the resource
        /// </summary>
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Indicates the status of the resource (valid, expired, pending deletion etc.)
        /// </summary>
        public ResourceStatus  Status { get; set; }
        

    }
}