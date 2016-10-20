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
    public class HomeController : Controller
    {
        private DataAccess db = new DataAccess();

        public ActionResult Index()
        {
            HomeIndexViewModel model = null;

            if (ClaimsPrincipal.Current.Identity.IsAuthenticated)
            {
                model = new HomeIndexViewModel();
                model.UserOrganizations = new Dictionary<string, Organization>();
                model.UserSubscriptions = new Dictionary<string, Subscription>();
                model.UserCanManageAccessForSubscriptions = new List<string>();
                model.DisconnectedUserOrganizations = new List<string>();
                model.UserId = AzureAuthUtils.GetSignedInUserUniqueName();

                var organizations = AzureResourceManagerUtil.GetUserOrganizations();
                foreach (Organization org in organizations)
                {
                    model.UserOrganizations.Add(org.Id, org);
                    var subscriptions = AzureResourceManagerUtil.GetUserSubscriptions(org.Id);

                    if (subscriptions != null)
                    {
                        var devSubscriptions = subscriptions.Where(s => s.DisplayName.Contains("Stage0"));
                     
                        foreach (var subscription in devSubscriptions)
                        {

                            Subscription s = db.Subscriptions.Find(subscription.Id);
                            if (s != null)
                            {
                                subscription.IsConnected = true;
                                subscription.ConnectedOn = s.ConnectedOn;
                                subscription.ConnectedBy = s.ConnectedBy;
                                subscription.AzureAccessNeedsToBeRepaired = !AzureResourceManagerUtil.ServicePrincipalHasReadAccessToSubscription(subscription.Id, org.Id);
                            }
                            else
                            {
                                subscription.IsConnected = false;
                            }

                            model.UserSubscriptions.Add(subscription.Id, subscription);
                            if (AzureResourceManagerUtil.UserCanManageAccessForSubscription(subscription.Id, org.Id))
                                model.UserCanManageAccessForSubscriptions.Add(subscription.Id);
                          
                        }
                    }
                    else
                        model.DisconnectedUserOrganizations.Add(org.Id);
                }
            }
            db.SaveChanges();

    
            return View(model);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "This is a personal project. Please contact me for any questions.";

            return View();
        }
    }
}