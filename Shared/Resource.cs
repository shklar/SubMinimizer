//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace CogsMinimizer.Shared
{
    using System;
    using System.Collections.Generic;
    
    public partial class Resource
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Resource()
        {
            this.Expired = false;
        }
    
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public bool ConfirmedOwner { get; set; }
        public string ResourceGroup { get; set; }
        public string Description { get; set; }
        public System.DateTime FirstFoundDate { get; set; }
        public System.DateTime ExpirationDate { get; set; }
        public System.DateTime LastVisitedDate { get; set; }
        public string AzureResourceIdentifier { get; set; }
        public string SubscriptionId { get; set; }
        public ResourceStatus Status { get; set; }
        public bool Expired { get; set; }
        public string Owner { get; set; }
    }
}
