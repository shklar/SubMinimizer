using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations.Model;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CogsMinimizer.Shared;

namespace CogsMinimizer.Models
{
    public class SubscriptionAnalyzeViewModel
    {
        public Subscription SubscriptionData { get; set; }

        public IEnumerable<Resource> Resources { get; set; }

        public SelectList GroupList { get; set; }
    }
}