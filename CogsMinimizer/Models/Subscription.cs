using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace CogsMinimizer.Models
{
    /// <summary>
    /// Represents a subscription that can be managed
    /// </summary>
    public class Subscription
    {
        public string Id { get; set; }
        [NotMapped]
        public string DisplayName { get; set; }
        public string OrganizationId { get; set; }
        [NotMapped]
        public bool IsConnected { get; set; }
        [NotMapped]
        public DateTime ConnectedOn { get; set; }
        [NotMapped]
        public string ConnectedBy { get; set; }
        [NotMapped]
        public bool AzureAccessNeedsToBeRepaired { get; set; }
    }
}