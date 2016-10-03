using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Authorization.Models;
using Microsoft.Azure.Management.ResourceManager.Models;

namespace CogsMinimizer.Shared
{
    public class SubscriptionAnalysis
    {

        /// <summary>
        /// The DB instance against which all updates and queries are made
        /// </summary>
        private DataAccess m_Db;

        /// <summary>
        /// The subscription that needs to be analyzed
        /// </summary>
        private Subscription m_analyzedSubscription;

        private SubscriptionAnalysisResult m_analysisResult;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dbAccess"></param>
        /// <param name="sub"></param>
        public SubscriptionAnalysis(DataAccess dbAccess, Subscription sub)
        {
            m_Db = dbAccess;
            m_analyzedSubscription = sub;

            m_analysisResult = new SubscriptionAnalysisResult();
        }

        /// <summary>
        /// Used to analyze the status of the resources within a given subscription
        /// which the application is expected to have access to
        /// </summary>
        public SubscriptionAnalysisResult AnalyzeSubscription()
        {
            //Record the start time of the analysis process
            DateTime startTime = DateTime.UtcNow;
            m_analysisResult.AnalysisStartTime = startTime;

            //Verify that the application indeed has access to the subscription
            bool isApplicationAutohrizedToReadSubscription =
                AzureResourceManagerUtil.ServicePrincipalHasReadAccessToSubscription(m_analyzedSubscription.Id,
                m_analyzedSubscription.OrganizationId);

            //Couldn't access the subcription
            if (!isApplicationAutohrizedToReadSubscription)
            {
                m_analysisResult.IsSubscriptionAccessible = false;
            }
            
            //Successfully accessed the subscription. Process the resources.
            else
            {
                AnalyzeSubscriptionResources();
            }

            //Record the end time of the analysis
            m_analysisResult.AnalysisEndTime = DateTime.UtcNow;
            return m_analysisResult;
        }

        private void AnalyzeSubscriptionResources()
        {
            var resources = new List<Resource>();
            var subscriptionAdmins = AzureResourceManagerUtil.GetSubscriptionAdmins(m_analyzedSubscription.Id,
                m_analyzedSubscription.OrganizationId);
            var emails = GetEmails(subscriptionAdmins);
            var adminEmails = emails.ToList();

            var resourceGroups = AzureResourceManagerUtil.GetResourceGroups(m_analyzedSubscription.Id, 
                m_analyzedSubscription.OrganizationId);

            //Go over all the resource groups
            foreach (var group in resourceGroups)
            {
                var resourceList = AzureResourceManagerUtil.GetResourceList(m_analyzedSubscription.Id,
                    m_analyzedSubscription.OrganizationId, group.Name);

                //Go over all the resource
                foreach (var genericResource in resourceList)
                {
                    //Try to find the resource in the DB
                    var resourceEntryFromDb = m_Db.Resources.FirstOrDefault(x => x.AzureResourceIdentifier.Equals(genericResource.Id));

                    //First time resource encountered
                    if (resourceEntryFromDb == null)
                    {
                        StoreNewFoundResource(genericResource, adminEmails, group.Name);
                    }

                    //Got this resource in the DB already
                    else
                    {
                        UpdateKnownResource(resourceEntryFromDb);
                    }              
                }
            }

            //Clean up any resources in the DB that were no longer found (probably deleted)
            var unvisitedResources = m_Db.Resources.Where(x => x.LastVisitedTime < m_analysisResult.AnalysisStartTime.Date);
            foreach (var unvisitedResource in unvisitedResources)
            {
                m_Db.Resources.Remove(unvisitedResource);
            }
        }

    
        /// <summary>
        /// Creates a new resource to be stored in the DB
        /// </summary>
        /// <param name="genericResource"></param>
        /// <param name="adminEmails"></param>
        /// <param name="groupName"></param>
        private void StoreNewFoundResource(GenericResource genericResource, List<string> adminEmails, string groupName)
        {
            var owner = FindOwner(genericResource.Name, adminEmails);

            var resource = new Resource
                           {
                               Id = Guid.NewGuid().ToString(),
                               AzureResourceIdentifier = genericResource.Id,
                               Name = genericResource.Name,
                               Type = genericResource.Type,
                               ResourceGroup = groupName,
                               FirstFoundDate = m_analysisResult.AnalysisStartTime.Date,
                               ExpirationDate =
                                   m_analysisResult.AnalysisStartTime.Date.Add(
                                       TimeSpan.FromDays(Subscription.DEFAULT_EXPIRATION_INTERVAL_IN_DAYS)),
                               Owner = owner,
                               ConfirmedOwner = false,
                               Expired = false,
                               SubscriptionId = m_analyzedSubscription.Id
                           };

            m_Db.Resources.Add(resource);
        }

        /// <summary>
        /// Updates the state of a previously encountered resource
        /// </summary>
        /// <param name="resourceEntryFromDb"></param>
        private void UpdateKnownResource(Resource resourceEntryFromDb)
        {
            //Resource is still good
            if (resourceEntryFromDb.ExpirationDate < m_analysisResult.AnalysisStartTime.Date)
            {
                //Resource has expired

                m_analysisResult.ExpiredResources.Add(resourceEntryFromDb);
            }

            //update the visit time
            resourceEntryFromDb.LastVisitedTime = m_analysisResult.AnalysisStartTime.Date;

            m_Db.Resources.AddOrUpdate(resourceEntryFromDb);
        }


        #region email string analysis
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

        private static string FindOwner(string resourceName, List<string> emails)
        {
            var owner = emails.FirstOrDefault(x => resourceName.Contains(GetAlias(x)));
            return owner;
        }
        #endregion
    }
}
