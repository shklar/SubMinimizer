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
    [Authorize]
    public class SubscriptionController : Controller
    {
        public const int EXPIRATION_INTERVAL_IN_DAYS = 7;

        // GET: Subscription
        
        public ActionResult Analyze([Bind(Include = "Id, OrganizationId, DisplayName")] Subscription subscription)
        {
            var resources = new List<Resource>();
            var subscriptionAdmins = AzureResourceManagerUtil.GetSubscriptionAdmins(subscription.Id,
                subscription.OrganizationId);
            var emails = GetEmails(subscriptionAdmins);
            var adminEmails = emails.ToList();

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
                        var owner = findOwner(genericResource.Name, adminEmails);
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

        private static string GetAlias(string email)
        {
           
            var alias = email.Substring(0, email.IndexOf('@'));
            return alias;
        }

        private static IEnumerable<string> GetEmails(IEnumerable<ClassicAdministrator> admins)
        {
            var emails = admins.Select(x => x.Properties.EmailAddress);
            return emails;
        }



        private static string findOwner(string resourceName, List<string> emails)
        {
            var owner = emails.FirstOrDefault(x=>resourceName.Contains(GetAlias(x)));
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
            return resource.ExpirationDate.Date < DateTime.UtcNow.Date;
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

        private DataAccess db = new DataAccess();

        public ActionResult Connect([Bind(Include = "Id, OrganizationId")] Subscription subscription, string servicePrincipalObjectId)
        {
            if (ModelState.IsValid)
            {
                AzureResourceManagerUtil.GrantRoleToServicePrincipalOnSubscription(servicePrincipalObjectId, subscription.Id, subscription.OrganizationId);
                if (AzureResourceManagerUtil.ServicePrincipalHasReadAccessToSubscription(subscription.Id, subscription.OrganizationId))
                {
                    subscription.ConnectedBy = (System.Security.Claims.ClaimsPrincipal.Current).FindFirst(ClaimTypes.Name).Value;
                    subscription.ConnectedOn = DateTime.UtcNow;

                    db.Subscriptions.AddOrUpdate(subscription);
                    db.SaveChanges();
                }
            }

            return RedirectToAction("Index", "Home");
        }
        public ActionResult Disconnect([Bind(Include = "Id, OrganizationId")] Subscription subscription, string servicePrincipalObjectId)
        {
            if (ModelState.IsValid)
            {
                AzureResourceManagerUtil.RevokeRoleFromServicePrincipalOnSubscription(servicePrincipalObjectId, subscription.Id, subscription.OrganizationId);

                Subscription s = db.Subscriptions.Find(subscription.Id);
                if (s != null)
                {
                    db.Subscriptions.Remove(s);
                    db.SaveChanges();
                }

            }

            return RedirectToAction("Index", "Home");
        }
        public ActionResult RepairAccess([Bind(Include = "Id, OrganizationId")] Subscription subscription, string servicePrincipalObjectId)
        {
            if (ModelState.IsValid)
            {
                AzureResourceManagerUtil.RevokeRoleFromServicePrincipalOnSubscription(servicePrincipalObjectId, subscription.Id, subscription.OrganizationId);
                AzureResourceManagerUtil.GrantRoleToServicePrincipalOnSubscription(servicePrincipalObjectId, subscription.Id, subscription.OrganizationId);
            }

            return RedirectToAction("Index", "Home");
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}