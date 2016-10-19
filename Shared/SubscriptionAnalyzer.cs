using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Authorization;
using Microsoft.Azure.Management.Authorization.Models;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.ResourceManager.Models;

namespace CogsMinimizer.Shared
{
    public class SubscriptionAnalyzer
    {

        /// <summary>
        /// The DB instance against which all updates and queries are made
        /// </summary>
        private DataAccess m_Db;

        /// <summary>
        /// The subscription that needs to be analyzed
        /// </summary>
        private Subscription m_analyzedSubscription;

        private readonly bool m_isOfflineMode;

        private SubscriptionAnalysisResult m_analysisResult;

        private ResourceManagementClient m_resourceManagementClient;

        private AuthorizationManagementClient m_authorizationManagementClient;

        private List<ClassicAdministrator> m_subscriptionAdmins;

        private ITracer _tracer;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dbAccess"></param>
        /// <param name="sub"></param>
        /// <param name="isOfflineMode">Indicates whether the analysis is run offline or with user interaction
        /// </param>
        public SubscriptionAnalyzer(DataAccess dbAccess, Subscription sub, bool isOfflineMode, ITracer tracer)
        {
            m_Db = dbAccess;
            m_analyzedSubscription = sub;
            m_isOfflineMode = isOfflineMode;
            _tracer = tracer;

            m_analysisResult = new SubscriptionAnalysisResult(sub);
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

            _tracer.TraceInformation($"Subscription Analysis started for {m_analyzedSubscription.DisplayName}");

            if (m_isOfflineMode)
            {
                m_resourceManagementClient = AzureResourceManagerUtil.GetAppResourceManagementClient(
                    m_analyzedSubscription.Id,
                    m_analyzedSubscription.OrganizationId);
                m_authorizationManagementClient = AzureResourceManagerUtil.GetAppAuthorizationManagementClient(
                    m_analyzedSubscription.Id,
                    m_analyzedSubscription.OrganizationId);
            }
            else 
            {
                m_resourceManagementClient = AzureResourceManagerUtil.GetUserResourceManagementClient(
                    m_analyzedSubscription.Id,
                    m_analyzedSubscription.OrganizationId);
                m_authorizationManagementClient = AzureResourceManagerUtil.GetUserAuthorizationManagementClient(
                    m_analyzedSubscription.Id,
                    m_analyzedSubscription.OrganizationId);
            }

            //Verify that the application indeed has access to the subscription
            bool isApplicationAutohrizedToForSubscription =
                AzureResourceManagerUtil.ServicePrincipalHasReadAccessToSubscription(m_analyzedSubscription.Id,
                m_analyzedSubscription.OrganizationId);

            //Couldn't access the subcription
            if (!isApplicationAutohrizedToForSubscription)
            {
                m_analysisResult.IsSubscriptionAccessible = false;
            }
            
            //Successfully accessed the subscription. Process the resources.
            else
            {
                m_analysisResult.IsSubscriptionAccessible = true;
                DeleteMarkedResources();
                AnalyzeSubscriptionResources();
            }

            //Record the end time of the analysis
            m_analysisResult.AnalysisEndTime = DateTime.UtcNow;

            _tracer.Flush();
            return m_analysisResult;
        }

        /// <summary>
        /// Deletes all the resources that were marked for deletion
        /// </summary>
        private void DeleteMarkedResources()
        {
            var resourcesMarkedForDeletion =
                m_Db.Resources.Where(x => x.SubscriptionId.Equals(m_analyzedSubscription.Id) &&
                x.Status == ResourceStatus.MarkedForDeletion).ToList();

            _tracer.TraceInformation($"Found {resourcesMarkedForDeletion.Count} resources for delete");

            //Try to delete the resources that are marked for delete
            foreach (var resource in resourcesMarkedForDeletion)
            {
                try
                {
                    _tracer.TraceVerbose($"Trying to delete the resource {resource.Name} of Type {resource.Type}");

                    AzureResourceManagerUtil.DeleteAzureResource(m_resourceManagementClient, resource.AzureResourceIdentifier, _tracer);
                    m_Db.Resources.Remove(resource);
                    m_analysisResult.DeletedResources.Add(resource);
                    _tracer.TraceVerbose($"Successfully deleted the resource {resource.Name} of Type {resource.Type}");

                }
                catch (Exception e)
                {
                    _tracer.TraceError($"Failed to delete the resource {resource.Name} of Type {resource.Type}. Exception details: {e.Message}");
                    
                    m_analysisResult.FailedDeleteResources.Add(resource);                  
                }
            }

            m_Db.SaveChanges();
        }

        /// <summary>
        /// Review all the resources in the subscription
        /// </summary>
        private void AnalyzeSubscriptionResources()
        {
            m_subscriptionAdmins = AzureResourceManagerUtil.GetSubscriptionAdmins(m_authorizationManagementClient).ToList();
            var emails = GetEmails(m_subscriptionAdmins);
            var adminEmails = emails.ToList();

            var resourceGroups = AzureResourceManagerUtil.GetResourceGroups(m_resourceManagementClient);

            //Go over all the resource groups
            foreach (var group in resourceGroups)
            {
                var resourceList = AzureResourceManagerUtil.GetResourceList(m_resourceManagementClient, group.Name);

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
                        UpdateKnownResource(resourceEntryFromDb, adminEmails);
                    }              
                }
            }

            m_Db.SaveChanges();

            //Clean up any resources in the DB that were no longer found (probably deleted)
            var unvisitedResources = m_Db.Resources.Where(x => x.SubscriptionId.Equals(m_analyzedSubscription.Id) &&
            x.LastVisitedDate < m_analysisResult.AnalysisStartTime.Date).ToList();

            m_analysisResult.NotFoundResources = unvisitedResources;

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
            var owner = FindOwner(genericResource.Name, groupName, adminEmails);

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
                               LastVisitedDate = m_analysisResult.AnalysisStartTime.Date,
                               Owner = owner,
                               ConfirmedOwner = false,
                               Expired = false,
                               SubscriptionId = m_analyzedSubscription.Id,
                               Status = ResourceStatus.Valid
                           };

            m_Db.Resources.Add(resource);
            m_analysisResult.NewResources.Add(resource);
        }

        /// <summary>
        /// Updates the state of a previously encountered resource
        /// </summary>
        /// <param name="resourceEntryFromDb"></param>
        /// <param name="adminEmails"></param>
        private void UpdateKnownResource(Resource resourceEntryFromDb, List<string> adminEmails)
        {
            //Try to update the owner if it is unknown
            if (String.IsNullOrWhiteSpace(resourceEntryFromDb.Owner))
            {
                var foundOwner = FindOwner(resourceEntryFromDb.Name, resourceEntryFromDb.ResourceGroup, adminEmails);
                if (foundOwner!= null)
                {
                    resourceEntryFromDb.Owner = foundOwner;
                }
            }

            //Check if resource has expired
            if (resourceEntryFromDb.ExpirationDate < m_analysisResult.AnalysisStartTime.Date)
            {
                //The resource status can't go from "marked for delete" to "Expired". Only from "Valid" to "Expired"
                if (resourceEntryFromDb.Status == ResourceStatus.Valid)
                {
                    resourceEntryFromDb.Status = ResourceStatus.Expired;
                }

                //Add to the list of expired resources even if it is marked for delete
                m_analysisResult.ExpiredResources.Add(resourceEntryFromDb);
            }

            //This is a plain valid resource
            else if (resourceEntryFromDb.Status == ResourceStatus.Valid)
            {
                m_analysisResult.ValidResources.Add(resourceEntryFromDb);
            }

            //update the visit time
            resourceEntryFromDb.LastVisitedDate = m_analysisResult.AnalysisStartTime.Date;
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

        private static string FindOwner(string resourceName, string groupName, List<string> emails)
        {
            var owner = emails.FirstOrDefault(x => (resourceName+groupName).Contains(GetAlias(x)));
            return owner;
        }
        #endregion
    }
}
