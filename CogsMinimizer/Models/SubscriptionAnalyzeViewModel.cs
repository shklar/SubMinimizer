using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations.Model;
using System.Linq;
using System.Web;

namespace CogsMinimizer.Models
{
    public class SubscriptionAnalyzeViewModel
    {
        public Subscription SubscriptionData { get; set; }

        public IEnumerable<Resource> Resources { get; set; }
    }
}