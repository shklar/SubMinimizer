using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations.Model;
using System.Linq;
using System.Web;
using CogsMinimizer.Shared;

namespace CogsMinimizer.Models
{
    public class EditSettingsViewModel
    {
        public string UserID { get; set; }

        public Subscription SubscriptionData { get; set; }

        public int DefaulExpiration { get; set; }

        public int DefaulExpirationUnclaimed { get; set; }

        public SubscriptionManagementLevel ManagementLevel { get; set; }

        public string SendEmailToCoadmins { get; set; }

        public EditSettingsViewModel()
        {
            this.DefaulExpiration = 10;
            this.DefaulExpirationUnclaimed = 10;
            this.SendEmailToCoadmins = "Yes";
            this.ManagementLevel = SubscriptionManagementLevel.ReportOnly;
        }
    }
}