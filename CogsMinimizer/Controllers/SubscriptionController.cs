using CogsMinimizer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CogsMinimizer.Controllers
{
    public class SubscriptionController : Controller
    {
        // GET: Subscription
        public ActionResult Analyze([Bind(Include = "Id, OrganizationId")] Subscription subscription, string servicePrincipalObjectId)
        {
            var model = new List<Resource>();
            var subscriptionAdmins = AzureResourceManagerUtil.GetSubscriptionAdmins(subscription.Id, subscription.OrganizationId);

            var resourceGroups = AzureResourceManagerUtil.GetResourceGroups(subscription.Id, subscription.OrganizationId);

            foreach (var group in resourceGroups)
            {
                var resourceList = AzureResourceManagerUtil.GetResourceList(subscription.Id, subscription.OrganizationId, group.Name);
                var resources = resourceList.Select(x => new Resource
                {
                    Name = x.Name,
                    Type = x.Type,
                    ResourceGroup = group.Name

                }
                    );
                model.AddRange(resources);
            }
           
           
            return View(model);
        }
    }
}