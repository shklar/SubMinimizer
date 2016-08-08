using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations.Model;
using System.Linq;
using System.Web;

namespace CogsMinimizer.Models
{
    public class Resource    
    {
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
        public string Owner { get; private set; }
        
        /// <summary>
        /// Indicates whether this resource is actually legitimate and should be kep
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        /// The resource Group to which this resource belongs
        /// </summary>
        public string ResourceGroup { get; set; }

    }
}