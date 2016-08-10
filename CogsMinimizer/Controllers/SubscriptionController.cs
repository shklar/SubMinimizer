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
        public ActionResult Analyze([Bind(Include = "Id, OrganizationId")] Subscription subscription, string servicePrincipalObjectId)
        {
            var model = new List<Resource>();
            var subscriptionAdmins = AzureResourceManagerUtil.GetSubscriptionAdmins(subscription.Id, subscription.OrganizationId);
            var aliases = GetAliases(subscriptionAdmins);
            var adminAliases = aliases.ToList();

            var resourceGroups = AzureResourceManagerUtil.GetResourceGroups(subscription.Id, subscription.OrganizationId);

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
                        var oldEntry = db.Resources.FirstOrDefault(x => x.Id.Equals(subscription.Id + "_" + group.Name + "_" + genericResource.Name));
                        DateTime firstEncountered = oldEntry == null ? DateTime.UtcNow.Date : oldEntry.FirstFound ;
                        

                        var resource = new Resource
                        {
                            Id = subscription.Id +"_" + group.Name+"_"+genericResource.Name,
                            Name = genericResource.Name,
                            Type = genericResource.Type,
                            ResourceGroup = group.Name,
                            FirstFound = firstEncountered,
                            Owner = owner
                        };
                        model.Add(resource);
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
    }
}