using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace CogsMinimizer.Shared
{
    /// <summary>
    /// Represents a subscription that can be managed
    /// </summary>
    public class Subscription
    {
        public const int DEFAULT_EXPIRATION_INTERVAL_IN_DAYS = 7;

        public string Id { get; set; }
       
        public string DisplayName { get; set; }
        public string OrganizationId { get; set; }
        [NotMapped]
        public bool IsConnected { get; set; }
       
        public DateTime ConnectedOn { get; set; }
     
        public string ConnectedBy { get; set; }

        [NotMapped]
        public bool AzureAccessNeedsToBeRepaired { get; set; }

        public DateTime LastAnalysisDate { get; set; }

        public int  ReserveIntervalInDays { get; set; }

        public int ExpirationIntervalInDays { get; set; }

        public int ExpirationUnclaimedIntervalInDays { get; set; }

        public int DeleteIntervalInDays { get; set; }

        public SubscriptionManagementLevel ManagementLevel { get; set; }

        public bool SendEmailToCoadmins { get; set; }

        public Subscription()
        {
            this.ReserveIntervalInDays = 180;
            this.ExpirationIntervalInDays = 30;
            this.ExpirationUnclaimedIntervalInDays = 10;
            this.DeleteIntervalInDays = 7;
            this.ManagementLevel = SubscriptionManagementLevel.ReportOnly;
            this.SendEmailToCoadmins = true;
        }
    }

  
}