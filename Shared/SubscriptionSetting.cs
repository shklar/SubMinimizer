using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CogsMinimizer.Shared
{
    /// <summary>
    /// Represents a subscription that can be managed
    /// </summary>
    public class SubscriptionSetting
    {
        [Key, Column(Order=0)]
        public string SubscriptionId { get; set; }

        [Key, Column(Order=1)]
        public string WebUserUniqueId { get; set; }

        public int DefaulExpiration { get; set; }

        public int DefaulExpirationUnclaimed { get; set; }

        public SubscriptionManagementLevel ManagementLevel { get; set; }

        public bool SendEmailToCoadmins { get; set; }

        public SubscriptionSetting()
        {
            this.DefaulExpiration = 10;
            this.DefaulExpirationUnclaimed = 10;
            this.SendEmailToCoadmins = true;
            this.ManagementLevel = SubscriptionManagementLevel.ReportOnly;
        }
    }
}