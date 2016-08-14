using CogsMinimizer.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.Azure.Management.Authorization.Models;
using Resource = CogsMinimizer.Models.Resource;

namespace CogsMinimizer.Controllers
{
    public class SubscriptionController : Controller
    {
        // GET: Subscription
        public ActionResult Analyze([Bind(Include = "Id, OrganizationId, DisplayName")] Subscription subscription, string servicePrincipalObjectId)
        {
            var resources = new List<Resource>();
            var subscriptionAdmins = AzureResourceManagerUtil.GetSubscriptionAdmins(subscription.Id, subscription.OrganizationId);
            var aliases = GetAliases(subscriptionAdmins);
            var adminAliases = aliases.ToList();

            var resourceGroups = AzureResourceManagerUtil.GetResourceGroups(subscription.Id, subscription.OrganizationId);

            var selectedResourceGroups = resourceGroups;

            using (var db = new DataAccess())
            {
                var oldResources = db.Resources;

                foreach (var group in selectedResourceGroups)
                {
                    var resourceList = AzureResourceManagerUtil.GetResourceList(subscription.Id,
                        subscription.OrganizationId, group.Name);
                    // var owner = findOwner(x.Name, adminAliases);
                    foreach (var genericResource in resourceList)
                    {
                        var owner = findOwner(genericResource.Name, adminAliases);
                        var oldEntry = db.Resources.FirstOrDefault(x => x.AzureResourceIdentifier.Equals(genericResource.Id));

                       var resource = oldEntry;

                        if (oldEntry==null)
                        {
                            resource = new Resource
                            {
                                Id = Guid.NewGuid().ToString(),
                                AzureResourceIdentifier = genericResource.Id,
                                Name = genericResource.Name,
                                Type = genericResource.Type,
                                ResourceGroup = group.Name,
                                FirstFoundDate = DateTime.UtcNow.Date,
                                Owner = owner,
                                Expired = false,
                                SubscriptionId = subscription.Id
                            };
                           
                        }
                        resource.Expired = DateTime.UtcNow.Date.Subtract(resource.FirstFoundDate).Days > 7;
                        resources.Add(resource);
                        db.Resources.AddOrUpdate(resource);
                    }
                }
                try
                {
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    //Do nothing
                }
            }

            var orderedResources = resources.OrderBy(x => x.FirstFoundDate);
            var model = new SubscriptionAnalyzeViewModel {Resources = orderedResources, SubscriptionData = subscription};
            return View(model);
        }

        private IEnumerable<string> GetAliases(IEnumerable<ClassicAdministrator> admins)
        {
            var aliases = admins.Select(x => x.Properties.EmailAddress);
            aliases = aliases.Select(x => x.Substring(0, x.IndexOf('@')));
            return aliases;
        }

        private static string findOwner(string resourceName, List<string> aliases)
        {
            var owner = aliases.FirstOrDefault(resourceName.Contains);
            return owner;
        }

        public ActionResult Delete(string subscriptionId, string AzureResourceId)
        {
            using (var db = new DataAccess())
            {
                var resource = db.Resources.FirstOrDefault(x => x.AzureResourceIdentifier.Equals(AzureResourceId));
                var subscription = db.Subscriptions.FirstOrDefault(x => x.Id.Equals(subscriptionId));
                if (subscription != null && resource != null)
                {
                    AzureResourceManagerUtil.DeleteResource(subscriptionId, subscription.OrganizationId, resource.ResourceGroup, resource.AzureResourceIdentifier);
                }
            }
            return RedirectToAction("Index", "Home");
        }

    }
}