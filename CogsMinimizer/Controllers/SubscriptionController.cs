using CogsMinimizer.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.IdentityModel.Claims;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.Azure.Management.Authorization.Models;
using Resource = CogsMinimizer.Models.Resource;

namespace CogsMinimizer.Controllers
{
    public class SubscriptionController : Controller
    {
        public const int EXPIRATION_INTERVAL_IN_DAYS = 7;

        // GET: Subscription
        public ActionResult Analyze([Bind(Include = "Id, OrganizationId, DisplayName")] Subscription subscription)
        {
            var resources = new List<Resource>();
            var subscriptionAdmins = AzureResourceManagerUtil.GetSubscriptionAdmins(subscription.Id,
                subscription.OrganizationId);
            var aliases = GetAliases(subscriptionAdmins);
            var adminAliases = aliases.ToList();

            var resourceGroups = AzureResourceManagerUtil.GetResourceGroups(subscription.Id, subscription.OrganizationId);

            //Todo: increase to all resource groups
            var selectedResourceGroups = resourceGroups.Take(1);

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
                        var oldEntry =
                            db.Resources.FirstOrDefault(x => x.AzureResourceIdentifier.Equals(genericResource.Id));

                        var resource = oldEntry;
                        var foundDate = DateTime.UtcNow.Date;
                        var exporatoinDate = foundDate.AddDays(EXPIRATION_INTERVAL_IN_DAYS);

                        if (oldEntry == null)
                        {
                            resource = new Resource
                            {
                                Id = Guid.NewGuid().ToString(),
                                AzureResourceIdentifier = genericResource.Id,
                                Name = genericResource.Name,
                                Type = genericResource.Type,
                                ResourceGroup = group.Name,
                                FirstFoundDate = foundDate,
                                ExpirationDate = exporatoinDate,
                                Owner = owner,
                                Expired = false,
                                SubscriptionId = subscription.Id
                            };

                        }
                        resource.Expired = HasExpired(resource);
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
            DeleteResource(subscriptionId, AzureResourceId);
            return RedirectToAction("Index", "Home");
        }

        private static void DeleteResource(string subscriptionId, string AzureResourceId)
        {
            using (var db = new DataAccess())
            {
                var resource = db.Resources.FirstOrDefault(x => x.AzureResourceIdentifier.Equals(AzureResourceId));
                var subscription = db.Subscriptions.FirstOrDefault(x => x.Id.Equals(subscriptionId));
                if (subscription != null && resource != null)
                {
                    AzureResourceManagerUtil.DeleteResource(subscriptionId, subscription.OrganizationId,
                        resource.ResourceGroup,
                        resource.AzureResourceIdentifier);
                }
            }
        }


        private bool HasExpired(Resource resource)
        {
            return DateTime.UtcNow.Subtract(resource.FirstFoundDate).Days > EXPIRATION_INTERVAL_IN_DAYS;
        }

        private static DateTime GetNewExpirationDate()
        {
           return DateTime.UtcNow.AddDays(EXPIRATION_INTERVAL_IN_DAYS);
        }

        public ActionResult DeleteExpired(string subscriptionId)
        {
            using (var db = new DataAccess())
            {
                var subResources = db.Resources.Where(x => x.SubscriptionId.Equals(subscriptionId)).ToList();
                var expiredResources = subResources.Where(HasExpired);

                foreach (var resource in expiredResources)
                {
                    DeleteResource(subscriptionId, resource.AzureResourceIdentifier);
                }
            }
            return RedirectToAction("Index", "Home");
        }

        public ActionResult Extend(string subscriptionId, string resourceId)
        {
            using (var db = new DataAccess())
            { 
                var resource = db.Resources.FirstOrDefault(x => x.SubscriptionId.Equals(subscriptionId) && x.Id.Equals(resourceId));
               
                if (resource != null)
                {
                    resource.Owner = AzureResourceManagerUtil.GetSignedInUserUniqueName();
                    resource.ExpirationDate= GetNewExpirationDate();
                }
                db.Resources.AddOrUpdate(resource);
                db.SaveChanges();
            }
            return RedirectToAction("Index", "Home");
        }
    }
}