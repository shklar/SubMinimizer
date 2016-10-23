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
        public ActionResult GetSettings([Bind(Include = "Id, OrganizationId, DisplayName")] Subscription subscription)
        {
            // By weird reason not all fields we need are set in subscription instance we get.
            // Even specified in index view they are absent here.
            // Let's find subscription in database
            using (DataAccess dataAccess = new DataAccess())
            {
                Subscription existingSubscription =
                    dataAccess.Subscriptions.Where<Subscription>(s => s.Id == subscription.Id).FirstOrDefault();
                if (existingSubscription == null)
                {
                    throw new ArgumentException(string.Format("No subscription found with ID {0}", subscription.Id));
                }

                return View(existingSubscription);
            }
        }

        [HttpPost]
        public ActionResult SaveSettings(Subscription subscription)
        {

            using (DataAccess dataAccess = new DataAccess())
            {
                Subscription existingSubscription =
                    dataAccess.Subscriptions.Where<Subscription>(s => s.Id == subscription.Id).FirstOrDefault();
                if (existingSubscription == null)
                {
                    throw new ArgumentException(string.Format("No subscription found with ID {0}", subscription.Id));
                }

                existingSubscription.ExpirationIntervalInDays = subscription.ExpirationIntervalInDays;
                existingSubscription.ExpirationUnclaimedIntervalInDays = subscription.ExpirationUnclaimedIntervalInDays;
                existingSubscription.ManagementLevel = subscription.ManagementLevel;
                existingSubscription.SendEmailToCoadmins = subscription.SendEmailToCoadmins;

                dataAccess.SaveChanges();

                return RedirectToAction("Index", "Home");
            }
        }


        public ActionResult CancelGetSettings([Bind(Include = "Id, OrganizationId, DisplayName")] Subscription subscription)
        {

           return RedirectToAction("Index", "Home");
        }
        
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
                var subscription = db.Subscriptions.FirstOrDefault(x => x.Id.Equals(subscriptionId));

                if (resource != null && subscription != null)
                {
                    resource.Owner = AzureAuthUtils.GetSignedInUserUniqueName();
                    resource.ExpirationDate = GetNewExpirationDate(subscription, resource);
                    resource.Status = ResourceStatus.Valid;
                }
                else
                {
                    throw new ArgumentException(string.Format("Can't find resource with ID '{0}' at subscription with ID '{1}'", resourceId, subscriptionId));
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



        /// <summary>
        /// Marks a single resource for delete
        /// </summary>
        /// <param name="subscriptionId"></param>
        /// <param name="resourceId"></param>
        /// <returns></returns>
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

        private static DateTime GetNewExpirationDate(Subscription  subscription, Resource resource)
        {
            
           return DateTime.UtcNow.AddDays(resource.Owner != null ? subscription.ExpirationIntervalInDays : subscription.ExpirationUnclaimedIntervalInDays);
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

        /// <summary>
        /// Extends all the expired resources in the subscription
        /// </summary>
        /// <param name="subscriptionId"></param>
        /// <returns></returns>
        public ActionResult ExtendExpired(string subscriptionId)
        {
            using (var db = new DataAccess())
            {
                var subResources = db.Resources.Where(x => x.SubscriptionId.Equals(subscriptionId)).ToList();
                var expiredResources = subResources.Where(HasExpired);

                foreach (var resource in expiredResources)
                {
                    var subscription = db.Subscriptions.FirstOrDefault(x => x.Id.Equals(subscriptionId));
                    resource.Owner = AzureAuthUtils.GetSignedInUserUniqueName();
                    resource.ExpirationDate = GetNewExpirationDate(subscription, resource);
                    resource.Status = ResourceStatus.Valid;
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