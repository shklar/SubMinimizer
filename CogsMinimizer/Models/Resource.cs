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
        /// The resource Group to which this resource belongs
        /// </summary>
        public string ResourceGroup { get; set; }

        /// <summary>
        /// The first date when the resource was encountered
        /// </summary>
        public DateTime FirstFound { get; set; }

    }
}