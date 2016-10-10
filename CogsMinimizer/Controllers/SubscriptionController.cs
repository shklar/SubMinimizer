using CogsMinimizer.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.IdentityModel.Claims;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CogsMinimizer.Shared;
using Microsoft.Azure.Management.Authorization.Models;
using Resource = CogsMinimizer.Shared.Resource;

namespace CogsMinimizer.Controllers
{
    [Authorize]
    public class SubscriptionController : Controller
    {
        public const int EXPIRATION_INTERVAL_IN_DAYS = 7;

        // GET: Subscription
        
        public ActionResult Analyze([Bind(Include = "Id, OrganizationId, DisplayName")] Subscription subscription)
        {       
            var model = GetResourcesViewModel(subscription.Id);
            return View(model);
        }

        //Extends the duration of a resource so that it does not get reported or deleted as expired
        public ActionResult Extend(string subscriptionId, string resourceId)
        {
            using (var db = new DataAccess())
            {
                var resource = db.Resources.FirstOrDefault(x => x.SubscriptionId.Equals(subscriptionId) && x.Id.Equals(resourceId));

                if (resource != null)
                {
                    resource.Owner = AzureAuthUtils.GetSignedInUserUniqueName();
                    resource.ExpirationDate = GetNewExpirationDate();
                    resource.Status = ResourceStatus.Valid;
                }
                db.Resources.AddOrUpdate(resource);
                db.SaveChanges();
            }

            var model = GetResourcesViewModel(subscriptionId);
            return View("Analyze",model);
        }

        private SubscriptionAnalyzeViewModel GetResourcesViewModel(string subscriptionId)
        {
            var resources = new List<Resource>();

            Subscription subscription = null;
            using (var db = new DataAccess())
            {
                var subscriptionResources = db.Resources.Where(x => x.SubscriptionId.Equals(subscriptionId));         
                resources.AddRange(subscriptionResources);
                subscription = db.Subscriptions.FirstOrDefault(x => x.Id.Equals(subscriptionId));
            }

            var orderedResources = resources.OrderBy(x => x.ExpirationDate);
            var model = new SubscriptionAnalyzeViewModel {Resources = orderedResources, SubscriptionData = subscription};
            return model;
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




        public ActionResult Delete(string subscriptionId, string resourceId)
        {
            using (var db = new DataAccess())
            {
                var resource = db.Resources.FirstOrDefault(x => x.SubscriptionId.Equals(subscriptionId) && x.Id.Equals(resourceId));

                if (resource != null)
                {
                    resource.Status = ResourceStatus.MarkedForDeletion;
                    db.Resources.AddOrUpdate(resource);
                    db.SaveChanges();
                }
            }

            var model = GetResourcesViewModel(subscriptionId);
            return View("Analyze", model);
        }

   


        private bool HasExpired(Resource resource)
        {
            return resource.ExpirationDate.Date < DateTime.UtcNow.Date;
        }

        private static DateTime GetNewExpirationDate()
        {
           return DateTime.UtcNow.AddDays(EXPIRATION_INTERVAL_IN_DAYS);
        }

        /// <summary>
        /// Marks all the expired resources in the subscription for deletion
        /// </summary>
        /// <param name="subscriptionId"></param>
        /// <returns></returns>
        public ActionResult DeleteExpired(string subscriptionId)
        {
            using (var db = new DataAccess())
            {
                var subResources = db.Resources.Where(x => x.SubscriptionId.Equals(subscriptionId)).ToList();
                var expiredResources = subResources.Where(HasExpired);

                foreach (var resource in expiredResources)
                {
                    resource.Status = ResourceStatus.MarkedForDeletion;
                    db.Resources.AddOrUpdate(resource);
                }
               
                db.SaveChanges();
            }

            var model = GetResourcesViewModel(subscriptionId);
            return View("Analyze", model);
        }

  

        private DataAccess db = new DataAccess();

        public ActionResult Connect([Bind(Include = "Id, OrganizationId, DisplayName")] Subscription subscription, string servicePrincipalObjectId)
        {
            if (ModelState.IsValid)
            {
                AzureResourceManagerUtil.GrantRoleToServicePrincipalOnSubscription(servicePrincipalObjectId, subscription.Id, subscription.OrganizationId);
                if (AzureResourceManagerUtil.ServicePrincipalHasReadAccessToSubscription(subscription.Id, subscription.OrganizationId))
                {
                    subscription.ConnectedBy = (System.Security.Claims.ClaimsPrincipal.Current).FindFirst(ClaimTypes.Name).Value;
                    subscription.ConnectedOn = DateTime.UtcNow;
                    subscription.IsConnected = true;
                    subscription.AzureAccessNeedsToBeRepaired = false;
                    subscription.LastAnalysisDate = DateTime.UtcNow.Date;

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

                    //Delete from the DB all resources in this subscription 
                    foreach (var resource in db.Resources.ToList())
                    {
                        if (resource.SubscriptionId.Equals(subscription.Id))
                        {
                            db.Resources.Remove(resource);
                        }
                    }

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