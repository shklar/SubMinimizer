using CogsMinimizer.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.IdentityModel.Claims;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CogsMinimizer.Shared;
using Microsoft.Azure.Management.Authorization.Models;
using Resource = CogsMinimizer.Shared.Resource;
using System.Configuration;

namespace CogsMinimizer.Controllers
{
    [Authorize]
    public class SubscriptionController : SubMinimizerController
    {
        public const int EXPIRATION_INTERVAL_IN_DAYS = 7;

        // Reset the duration of all subscription resources ( removes it's confirmed owner and sets its expiration date to today + unclaimed resources expiration date)
        [HttpPost]
        public ActionResult ResetResources(string SubscriptionId)
        {
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => SubscriptionId);

            List<object> resultList = new List<object>();
            using (DataAccess db = new DataAccess())
            {
                var subscription = db.Subscriptions.FirstOrDefault(x => x.Id.Equals(SubscriptionId));

                if (subscription == null)
                {
                    throw new ArgumentException(string.Format("Subscription with ID '{0}' wasn't found.", SubscriptionId));
                }

                string currentUser = AzureAuthUtils.GetSignedInUserUniqueName();
                if (currentUser != subscription.ConnectedBy)
                {
                    throw new ArgumentException("You are not authorized to  reset resources at this subscription.Please contact the subscription owner");
                }

                // Let's compose result list in order to fill resources table at view
                List<Resource> subscriptionResourceList = db.Resources.Where(r => r.SubscriptionId == SubscriptionId).ToList();

                // Let's compose result list in order to fill resources table at view
                foreach (Resource resource in subscriptionResourceList)
                {

                    // Reset expiration date for resources of selected subscription
                    ResourceOperationsUtil.ResetResource(resource, subscription);

                    db.Resources.AddOrUpdate(resource);

                    resultList.Add(new
                    {
                        Id = resource.Id,
                        Status = resource.Status.ToString(),
                        ConfirmedOwner = resource.ConfirmedOwner,
                        ExpirationDate = resource.ExpirationDate.ToShortDateString(),
                    });
                }

                db.SaveChanges();

                return Json(resultList);
            }
        }

        // GET: Subscription
        public ActionResult GetSettings(string subscriptionId)
        {
            Diagnostics.EnsureArgumentNotNull(() => subscriptionId);

            using (DataAccess dataAccess = new DataAccess())
            { 
                Subscription existingSubscription =
                    dataAccess.Subscriptions.Where<Subscription>(s => s.Id.Equals(subscriptionId)).FirstOrDefault();
                if (existingSubscription == null)
                {
                    throw new ArgumentException(string.Format("Subscription with ID '{0}' wasn't found.", subscriptionId));
                }

                return View(existingSubscription);
            }
        }

        [HttpPost]
        public ActionResult SaveSettings(Subscription subscription)
        {
            Diagnostics.EnsureArgumentNotNull(() => subscription);

            using (DataAccess dataAccess = new DataAccess())
            {
                Subscription existingSubscription =
                    dataAccess.Subscriptions.Where<Subscription>(s => s.Id.Equals(subscription.Id)).FirstOrDefault();
                if (existingSubscription == null)
                {
                    throw new ArgumentException(string.Format("Subscription with ID '{0}' wasn't found.", subscription.Id));
                }

                string currentUser = AzureAuthUtils.GetSignedInUserUniqueName();
                if (currentUser != existingSubscription.ConnectedBy)
                {
                    throw new ArgumentException("You are not authorized to edit the subscription settings. Please contact the subscription owner");
                }

                /*
                // This section was used when the service issued delegated requests to ARM on behalf of the user 

                string servicePrincipalObjectId = ServicePrincipalObjectId;
                if (String.IsNullOrEmpty(servicePrincipalObjectId))                {
                    var organizations = AzureResourceManagerUtil.GetUserOrganizations();
                    foreach (var org in organizations)
                    {
                        var subscriptions = AzureResourceManagerUtil.GetUserSubscriptions(org.Id);
                        foreach (var sub in subscriptions)
                        {
                            if (sub.Id.Equals(subscription.Id))
                            {
                                servicePrincipalObjectId = org.objectIdOfCloudSenseServicePrincipal;
                                break;
                            }
                        }
                    }
                }
                
                if (String.IsNullOrEmpty(servicePrincipalObjectId))
                {
                    throw new ArgumentException(string.Format("Service principal with ID '{0}' wasn't found.", servicePrincipalObjectId));
                }
                */

                existingSubscription.ReserveIntervalInDays = subscription.ReserveIntervalInDays;
                existingSubscription.ExpirationIntervalInDays = subscription.ExpirationIntervalInDays;
                existingSubscription.ExpirationUnclaimedIntervalInDays = subscription.ExpirationUnclaimedIntervalInDays;
                existingSubscription.DeleteIntervalInDays = subscription.DeleteIntervalInDays;
                existingSubscription.ManagementLevel = subscription.ManagementLevel;
                existingSubscription.SendEmailToCoadmins = subscription.SendEmailToCoadmins;
                existingSubscription.SendEmailOnlyInvalidResources = subscription.SendEmailOnlyInvalidResources;

                //validate additional email recepients user input
                if (existingSubscription.AdditionalRecipients == null ||
                    !existingSubscription.AdditionalRecipients.Equals(subscription.AdditionalRecipients))
                {
                    existingSubscription.AdditionalRecipients = EmailAddressUtils.ValidateAndNormalizeEmails(subscription.AdditionalRecipients);
                }

                dataAccess.Subscriptions.AddOrUpdate<Subscription>(existingSubscription);
                dataAccess.SaveChanges();

                //We no longer manage permissions to the subscription on behalf of the user
                //AzureResourceManagementRole role = AzureResourceManagerUtil.GetNeededAzureResourceManagementRole(existingSubscription.ManagementLevel);
                //AzureResourceManagerUtil.RevokeAllRolesFromServicePrincipalOnSubscription(servicePrincipalObjectId, existingSubscription.Id, existingSubscription.OrganizationId);
                //AzureResourceManagerUtil.GrantRoleToServicePrincipalOnSubscription(servicePrincipalObjectId, existingSubscription.Id, existingSubscription.OrganizationId, role);

                return RedirectToAction("Index", "Home");
            }
        }


        public ActionResult CancelGetSettings()
        {

           return RedirectToAction("Index", "Home");
        }
        
        public ActionResult Analyze([Bind(Include = "Id")] Subscription subscription)
        {
            Diagnostics.EnsureArgumentNotNull(() => subscription);

            var model = GetResourcesViewModel(subscription.Id);
            ViewData["UserId"] = AzureAuthUtils.GetSignedInUserUniqueName();
            return View(model);
        }

        // Reserves the duration of a resource so that it does not get reported or deleted as expired
        [HttpPost]
        public ActionResult ReserveResource(string ResourceId, string SubscriptionId)
        {
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => ResourceId);
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => SubscriptionId);

            JsonResult result = new JsonResult();
            using (var db = new DataAccess())
            {
                var resource = db.Resources.FirstOrDefault(x => x.SubscriptionId.Equals(SubscriptionId) && x.Id.Equals(ResourceId));
                var subscription = db.Subscriptions.FirstOrDefault(x => x.Id.Equals(SubscriptionId));

                if (subscription == null)
                {
                    throw new ArgumentException(string.Format("Subscription with ID '{0}' wasn't found.", SubscriptionId));
                }

                if (resource == null)
                {
                    throw new ArgumentException(string.Format("Resource with ID '{0}' wasn't found.", ResourceId));
                }

                resource.Owner = AzureAuthUtils.GetSignedInUserUniqueName();
                resource.ConfirmedOwner = true;
                resource.ExpirationDate = ResourceOperationsUtil.GetNewReserveDate(subscription, resource);
                resource.Status = ResourceStatus.Valid;
                result.Data = new { ConfirmedOwner = resource.ConfirmedOwner, Owner = resource.Owner, ResourceId = resource.Id,  SubscriptionId = resource.SubscriptionId, ExpirationDate = resource.ExpirationDate.ToShortDateString(), Status = resource.Status.ToString() };

                db.Resources.AddOrUpdate(resource);
                db.SaveChanges();
            }

            return result;
        }

        // Edit resource description
        [HttpPost]
        public ActionResult SaveResourceDescription(string ResourceId, string SubscriptionId, string Description)
        {
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => ResourceId);
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => SubscriptionId);

            JsonResult result = new JsonResult();
            using (var db = new DataAccess())
            {
                var resource = db.Resources.FirstOrDefault(x => x.SubscriptionId.Equals(SubscriptionId) && x.Id.Equals(ResourceId));
                var subscription = db.Subscriptions.FirstOrDefault(x => x.Id.Equals(SubscriptionId));

                if (subscription == null)
                {
                    throw new ArgumentException(string.Format("Subscription with ID '{0}' wasn't found.", SubscriptionId));
                }

                if (resource == null)
                {
                    throw new ArgumentException(string.Format("Resource with ID '{0}' wasn't found.", ResourceId));
                }

                resource.Description = Description;
                result.Data = resource;

                db.Resources.AddOrUpdate(resource);
                db.SaveChanges();
            }

            return result;
        }


        //Reset the duration of a resource ( removes it's confirmed owner and sets its expiration date to today + unclaimed resources expiration date)
        [HttpPost]
        public ActionResult ResetResource(string ResourceId, string SubscriptionId)
        {
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => ResourceId);
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => SubscriptionId);

            JsonResult result = new JsonResult();
            using (var db = new DataAccess())
            {
                var resource = db.Resources.FirstOrDefault(x => x.SubscriptionId.Equals(SubscriptionId) && x.Id.Equals(ResourceId));
                var subscription = db.Subscriptions.FirstOrDefault(x => x.Id.Equals(SubscriptionId));

                if (subscription == null)
                {
                    throw new ArgumentException(string.Format("Subscription with ID '{0}' wasn't found.", SubscriptionId));
                }

                if (resource == null)
                {
                    throw new ArgumentException(string.Format("Resource with ID '{0}' wasn't found.", ResourceId));
                }

                string currentUser = AzureAuthUtils.GetSignedInUserUniqueName();
                if (currentUser != subscription.ConnectedBy)
                {
                    throw new ArgumentException("You are not authorized to  reset resources at this subscription.Please contact the subscription owner");
                }

                resource.ConfirmedOwner = false;

                resource.ExpirationDate = ResourceOperationsUtil.GetNewExpirationDate(subscription, resource);
                resource.Status = ResourceStatus.Valid;
                result.Data = new { ConfirmedOwner = resource.ConfirmedOwner, Owner = resource.Owner, ResourceId = resource.Id, SubscriptionId = resource.SubscriptionId, ExpirationDate = resource.ExpirationDate.ToShortDateString(), Status = resource.Status.ToString() };

                db.Resources.AddOrUpdate(resource);
                db.SaveChanges();
            }

            return result;
        }

        //Extends the duration of a resource so that it does not get reported or deleted as expired
        [HttpPost]
        public ActionResult ExtendResource(string ResourceId, string SubscriptionId)
        {
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => ResourceId);
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => SubscriptionId);

            JsonResult result = new JsonResult();
            using (var db = new DataAccess())
            {
                var resource = db.Resources.FirstOrDefault(x => x.SubscriptionId.Equals(SubscriptionId) && x.Id.Equals(ResourceId));
                var subscription = db.Subscriptions.FirstOrDefault(x => x.Id.Equals(SubscriptionId));

                if (subscription == null)
                {
                    throw new ArgumentException(string.Format("Subscription with ID '{0}' wasn't found.", SubscriptionId));
                }

                if (resource == null)
                {
                    throw new ArgumentException(string.Format("Resource with ID '{0}' wasn't found.", ResourceId));
                }

                resource.Owner = AzureAuthUtils.GetSignedInUserUniqueName();
                resource.ConfirmedOwner = true;

                resource.ExpirationDate = ResourceOperationsUtil.GetNewExpirationDate(subscription, resource);
                resource.Status = ResourceStatus.Valid;
                result.Data = new { ConfirmedOwner = resource.ConfirmedOwner, Owner = resource.Owner, ResourceId = resource.Id,  SubscriptionId = resource.SubscriptionId, ExpirationDate = resource.ExpirationDate.ToShortDateString(), Status = resource.Status.ToString() };

                db.Resources.AddOrUpdate(resource);
                db.SaveChanges();
            }

            return result;
        }

        private SubscriptionAnalyzeViewModel GetResourcesViewModel(string subscriptionId)
        {
            var resources = new List<Resource>();

            Subscription subscription = null;
            using (var db = new DataAccess())
            {
                subscription = db.Subscriptions.FirstOrDefault(x => x.Id.Equals(subscriptionId));
          
                var subscriptionResources = db.Resources.Where(x => x.SubscriptionId.Equals(subscriptionId));         
                resources.AddRange(subscriptionResources);
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
        private ActionResult Delete(string subscriptionId, string resourceId)
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
                else
                {
                    throw new ArgumentException(string.Format("Resource with ID '{0}' wasn't found.", resourceId));
                }

            }

            var model = GetResourcesViewModel(subscriptionId);
            return View("Analyze", model);
        }

        /// <summary>
        /// Marks all the expired resources in the subscription for deletion
        /// </summary>
        /// <param name="subscriptionId"></param>
        /// <returns></returns>
        private ActionResult DeleteExpired(string subscriptionId)
        {
            using (var db = new DataAccess())
            {
                var subResources = db.Resources.Where(x => x.SubscriptionId.Equals(subscriptionId)).ToList();
                var expiredResources = subResources.Where(r => ResourceOperationsUtil.HasExpired(r));

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
        private ActionResult ExtendExpired(string subscriptionId)
        {
            using (var db = new DataAccess())
            {
                var subscription = db.Subscriptions.FirstOrDefault(x => x.Id.Equals(subscriptionId));
                if (subscription == null)
                {
                    throw new ArgumentException(string.Format("Subscription with ID '{0}' wasn't found.", subscriptionId));
                }

                var subResources = db.Resources.Where(x => x.SubscriptionId.Equals(subscriptionId)).ToList();
                var expiredResources = subResources.Where(r => ResourceOperationsUtil.HasExpired(r));

                foreach (var resource in expiredResources)
                {
                    resource.Owner = AzureAuthUtils.GetSignedInUserUniqueName();
                    resource.ExpirationDate = ResourceOperationsUtil.GetNewExpirationDate(subscription, resource);
                    resource.Status = ResourceStatus.Valid;
                }

                db.SaveChanges();
            }

            var model = GetResourcesViewModel(subscriptionId);
            return View("Analyze", model);
        }



        private DataAccess db = new DataAccess();

        public ActionResult Register()
        {
            return View();
        }


        private ActionResult Connect([Bind(Include = "Id, OrganizationId, DisplayName")] Subscription subscription, string servicePrincipalObjectId)
        {
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => servicePrincipalObjectId);
            Diagnostics.EnsureArgumentNotNull(() => subscription);

            if (ModelState.IsValid)
            {
                //A new subscription is created with ReportOnly mode by default
                AzureResourceManagementRole role = AzureResourceManagerUtil.GetNeededAzureResourceManagementRole(SubscriptionManagementLevel.ReportOnly);

                // Grant the subscription the needed role
                AzureResourceManagerUtil.GrantRoleToServicePrincipalOnSubscription(servicePrincipalObjectId, subscription.Id, subscription.OrganizationId, role);
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
                else
                {
                    throw new ArgumentException(string.Format("Unable to monitor subscription with ID '{0}'.", subscription.Id));
                }

            }

            return RedirectToAction("Index", "Home");
        }

        public ActionResult ConnectNoArm([Bind(Include = "Id, DisplayName")] Subscription subscription)
        {
            Diagnostics.EnsureArgumentNotNull(() => subscription);

            if (ModelState.IsValid)
            {
                //Check whether the subscription is already monitored
                Subscription existingSubscription = 
                    db.Subscriptions.Where<Subscription>(s => s.Id.Equals(subscription.Id)).FirstOrDefault();
                if (existingSubscription != null)
                {
                    throw new ArgumentException(string.Format("Subscription already registered"));
                }

                subscription.OrganizationId = ConfigurationManager.AppSettings["env:TenantAADID"];

                // Ensure the service has read permission to the subscription
                if (AzureResourceManagerUtil.ServicePrincipalHasReadAccessToSubscription(subscription.Id, subscription.OrganizationId))
                {                  
                    //Create a new entry in the DB
                    subscription.ConnectedBy = AzureAuthUtils.GetSignedInUserUniqueName();
                    subscription.ConnectedOn = DateTime.UtcNow;
                    subscription.IsConnected = true;
                    subscription.AzureAccessNeedsToBeRepaired = false;
                    subscription.LastAnalysisDate = DateTime.UtcNow.Date;
                    db.Subscriptions.AddOrUpdate(subscription);
                    db.SaveChanges();
                }
                else
                {
                    throw new ArgumentException(string.Format("Unable to monitor subscription with ID '{0}'. " +
                        "Please make sure you have assigned reader role to SubMinimizer for this subscription", subscription.Id));
                }
            }
            return RedirectToAction("Index", "Home");
        }

        public ActionResult Disconnect([Bind(Include = "Id, OrganizationId")] Subscription subscription, string servicePrincipalObjectId)
        {
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => servicePrincipalObjectId);
            Diagnostics.EnsureArgumentNotNull(() => subscription);

            if (ModelState.IsValid)
            {                             
                Subscription s = db.Subscriptions.Find(subscription.Id);
                if (s != null)
                {
                    string currentUser = AzureAuthUtils.GetSignedInUserUniqueName();
                    if (currentUser != s.ConnectedBy)
                    {
                        throw new ArgumentException("You are not authorized to stop monitoring this subscription.Please contact the subscription owner");
                    }

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
                else
                {
                    throw new ArgumentException(string.Format("Subscription with ID '{0}' wasn't found.", subscription.Id));
                }
            }
            return RedirectToAction("Index", "Home");
        }

        public ActionResult RepairAccess([Bind(Include = "Id, OrganizationId")] Subscription subscription, string servicePrincipalObjectId)
        {
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => servicePrincipalObjectId);
            Diagnostics.EnsureArgumentNotNull(() => subscription);

            if (ModelState.IsValid)
            {
                AzureResourceManagerUtil.RevokeAllRolesFromServicePrincipalOnSubscription(servicePrincipalObjectId, subscription.Id, subscription.OrganizationId);

                var existingSubscription = db.Subscriptions.FirstOrDefault(x => x.Id.Equals(subscription.Id));
                if (existingSubscription == null)
                {
                    throw new ArgumentException(string.Format("Subscription with ID '{0}' wasn't found.", subscription.Id));
                }
                AzureResourceManagementRole role = AzureResourceManagerUtil.GetNeededAzureResourceManagementRole(existingSubscription.ManagementLevel);

                AzureResourceManagerUtil.GrantRoleToServicePrincipalOnSubscription(servicePrincipalObjectId, subscription.Id, subscription.OrganizationId, role);
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
