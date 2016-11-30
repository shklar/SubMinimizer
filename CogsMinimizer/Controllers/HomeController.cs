using CogsMinimizer.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using CogsMinimizer.Shared;

namespace CogsMinimizer.Controllers
{
    public class HomeController : SubMinimizerController
    {
        private DataAccess db = new DataAccess();

        public ActionResult Index()
        {
            HomeIndexViewModel model = null;

            if (ClaimsPrincipal.Current.Identity.IsAuthenticated)
            {
                var userName = ClaimsPrincipal.Current.Identity.Name;
                System.Diagnostics.Trace.TraceInformation($"Home/Index opened by {userName}");
                model = new HomeIndexViewModel();
                model.UserOrganizations = new Dictionary<string, Organization>();
                model.UserSubscriptions = new Dictionary<string, Subscription>();
                model.UserCanManageAccessForSubscriptions = new List<string>();
                model.DisconnectedUserOrganizations = new List<string>();

                var organizations = AzureResourceManagerUtil.GetUserOrganizations();
              
                foreach (Organization org in organizations)
                {
                    model.UserOrganizations.Add(org.Id, org);
                    var subscriptions = AzureResourceManagerUtil.GetUserSubscriptions(org.Id);

                    System.Diagnostics.Trace.TraceInformation($"Found {subscriptions.Count} subscriptions for {userName} ");

                    if (subscriptions != null)
                    {
                        //var devSubscriptions = subscriptions.Where(s => s.DisplayName.Contains("Stage0"));
                        var devSubscriptions = subscriptions;
                        var dbSubscriptions = db.Subscriptions.ToList();

                        foreach (var subscription in devSubscriptions)
                        {

                            Subscription s = dbSubscriptions.FirstOrDefault(x=> x.Id.Equals(subscription.Id));
                            if (s != null)
                            {
                                subscription.IsConnected = true;
                                subscription.ConnectedOn = s.ConnectedOn;
                                subscription.ConnectedBy = s.ConnectedBy;
                                subscription.ReserveIntervalInDays = s.ReserveIntervalInDays;
                                subscription.ExpirationIntervalInDays = s.ExpirationIntervalInDays;
                                subscription.ExpirationUnclaimedIntervalInDays = s.ExpirationUnclaimedIntervalInDays;
                                subscription.ManagementLevel = s.ManagementLevel;
                                subscription.SendEmailToCoadmins = s.SendEmailToCoadmins;
                                subscription.SendEmailOnlyValidResources = s.SendEmailOnlyValidResources;
                                //subscription.AzureAccessNeedsToBeRepaired = !AzureResourceManagerUtil.ServicePrincipalHasReadAccessToSubscription(subscription.Id, org.Id);
                                subscription.AzureAccessNeedsToBeRepaired = false;

                            }
                            else
                            {
                                subscription.IsConnected = false;
                            }

                            model.UserSubscriptions.Add(subscription.Id, subscription);
                            model.UserCanManageAccessForSubscriptions.Add(subscription.Id);
                            //if (AzureResourceManagerUtil.UserCanManageAccessForSubscription(subscription.Id, org.Id)) model.UserCanManageAccessForSubscriptions.Add(subscription.Id);
                          
                        }
                    }
                    else
                        model.DisconnectedUserOrganizations.Add(org.Id);
                }
            }
            db.SaveChanges();

    
            return View(model);
        }

        public ActionResult Error(string Exception)
        {
            ViewData["Exception"] = Exception;
            return View();
        }

        public ActionResult About()
        {
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "This is a personal project. Please contact me for any questions.";

            return View();
        }
    }
}