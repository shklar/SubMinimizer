using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Linq;
using Microsoft.Azure.Management.Authorization;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.ResourceManager.Models;

namespace CogsMinimizer.Shared
{
    public class SubscriptionAnalyzer
    {

        /// <summary>
        /// The DB instance against which all updates and queries are made
        /// </summary>
        private IDataAccess m_Db;

        /// <summary>
        /// The subscription that needs to be analyzed
        /// </summary>
        private Subscription m_analyzedSubscription;

        private IAzureResourceManagement m_azureResourceManagement;

        private SubscriptionAnalysisResult m_analysisResult;

        private List<string> m_subscriptionAdmins;

        private ITracer _tracer;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dbAccess"></param>
        /// <param name="sub"></param>
        /// </param>
        public SubscriptionAnalyzer(IDataAccess dbAccess, Subscription sub,
            IAzureResourceManagement azureManagement, ITracer tracer)
        {
            Diagnostics.EnsureArgumentNotNull(() => sub);
            Diagnostics.EnsureArgumentNotNull(() => tracer);
            Diagnostics.EnsureArgumentNotNull(() => dbAccess);

            m_Db = dbAccess;
            m_analyzedSubscription = sub;
            m_azureResourceManagement = azureManagement;
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

            //Verify that the application indeed has access to the subscription
            bool isApplicationAutohrizedToReadSubscription =
                m_azureResourceManagement.ServicePrincipalHasReadAccessToSubscription(m_analyzedSubscription.Id,
                m_analyzedSubscription.OrganizationId);

            //Couldn't access the subcription
            if (!isApplicationAutohrizedToReadSubscription)
            {
                _tracer.TraceInformation("Failed to get read access to subscription");
                m_analysisResult.IsSubscriptionAccessible = false;
            }

            //Successfully accessed the subscription. Process the resources.
            else
            {
                _tracer.TraceInformation("Have access to subscription");
                m_analysisResult.IsSubscriptionAccessible = true;

                _tracer.TraceInformation($"Subscription management level: {m_analyzedSubscription.ManagementLevel}");

                AnalyzeSubscriptionResources();
            }

            //Record the end time of the analysis
            m_analysisResult.AnalysisEndTime = DateTime.UtcNow;

            _tracer.Flush();
            return m_analysisResult;
        }

        /// <summary>
        /// Review all the resources in the subscription
        /// </summary>
        private void AnalyzeSubscriptionResources()
        {
            m_subscriptionAdmins = m_azureResourceManagement.GetSubscriptionAdmins(m_analyzedSubscription.Id, m_analyzedSubscription.OrganizationId);
            m_analysisResult.Admins = m_subscriptionAdmins;

            var emails = m_subscriptionAdmins;
            var adminEmails = emails.ToList();

            var resourceGroups = m_azureResourceManagement.GetResourceGroups();

            //Go over all the resource groups
            foreach (var group in resourceGroups)
            {
                var resourceList = m_azureResourceManagement.GetResourceList(group.Name);

                //Go over all the resource
                foreach (var genericResource in resourceList)
                {
                    //Try to find the resource in the DB
                    var resourceEntryFromDb = m_Db.Resources.FirstOrDefault(x => x.AzureResourceIdentifier.Equals(genericResource.Id));

                    //First time resource encountered
                    if (resourceEntryFromDb == null)
                    {
                        _tracer.TraceVerbose($"Found unknown resource: {genericResource.Name}");

                        StoreNewFoundResource(genericResource, adminEmails, group.Name);
                    }

                    //Got this resource in the DB already
                    else
                    {
                        _tracer.TraceVerbose($"Found known resource: {genericResource.Name}");
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
                                       TimeSpan.FromDays(
                                           owner != null ? m_analyzedSubscription.ExpirationIntervalInDays : m_analyzedSubscription.ExpirationUnclaimedIntervalInDays)),
                LastVisitedDate = m_analysisResult.AnalysisStartTime.Date,
                Owner = owner,
                ConfirmedOwner = false,
                Expired = false,
                SubscriptionId = m_analyzedSubscription.Id,
                Status = ResourceStatus.Valid
            };

            _tracer.TraceVerbose($"Found New resource: {resource.Name}");

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
            _tracer.TraceVerbose($"Trying to determine resource owner for {resourceEntryFromDb.Name}");

            //Try to update the owner if it is unknown
            if (String.IsNullOrWhiteSpace(resourceEntryFromDb.Owner))
            {
                var foundOwner = FindOwner(resourceEntryFromDb.Name, resourceEntryFromDb.ResourceGroup, adminEmails);
                if (foundOwner != null)
                {
                    resourceEntryFromDb.Owner = foundOwner;
                    _tracer.TraceVerbose($"Found owner for {resourceEntryFromDb.Name} Owner: {resourceEntryFromDb.Owner}");
                }
                else
                {
                    _tracer.TraceVerbose($"Couldn't determine resource owner for {resourceEntryFromDb.Name}");
                }
            }

            //Check if resource has expired
            var resourceExpirationAge = (resourceEntryFromDb.ExpirationDate - m_analysisResult.AnalysisStartTime.Date).TotalDays;
            if (resourceExpirationAge < 0)
            {
                _tracer.TraceVerbose($"Found expired resource: {resourceEntryFromDb.Name} expiration date {resourceEntryFromDb.ExpirationDate}");

                //The resource status can't go from "marked for delete" to "Expired". Only from "Valid" to "Expired"
                if (resourceEntryFromDb.Status == ResourceStatus.Valid)
                {
                    resourceEntryFromDb.Status = ResourceStatus.Expired;
                    _tracer.TraceVerbose($"Assigning expired resource: {resourceEntryFromDb.Name} with status {resourceEntryFromDb.Status}");
                    
                    //Add to the list of expired resources
                    m_analysisResult.ExpiredResources.Add(resourceEntryFromDb);
                }

               else if (resourceEntryFromDb.Status == ResourceStatus.Expired)
                {
                    if (resourceExpirationAge <= -m_analyzedSubscription.DeleteIntervalInDays)
                    {
                        resourceEntryFromDb.Status = ResourceStatus.MarkedForDeletion;
                        _tracer.TraceVerbose($"Mark for delete time reached. Assigning expired resource: {resourceEntryFromDb.Name} with status {resourceEntryFromDb.Status}");

                        //Add to the list of marked for delete resources
                        m_analysisResult.MarkedForDeleteResources.Add(resourceEntryFromDb);
                    }

                    //This resource remains expired
                    else
                    {
                        _tracer.TraceVerbose($"Resource remains expired: {resourceEntryFromDb.Name}");

                        //Add to the list of expired resources
                        m_analysisResult.ExpiredResources.Add(resourceEntryFromDb);

                    }
                }
               
                // The resource was already marked for delete
                else if (resourceEntryFromDb.Status == ResourceStatus.MarkedForDeletion)
                {
                    _tracer.TraceVerbose($"Resource remains marked for delete: {resourceEntryFromDb.Name}");

                    //Add to the list of marked for delete resources
                    m_analysisResult.MarkedForDeleteResources.Add(resourceEntryFromDb);
                }
                
            }

            //This is a plain valid resource
            else if (resourceEntryFromDb.Status == ResourceStatus.Valid)
            {
                m_analysisResult.ValidResources.Add(resourceEntryFromDb);
                _tracer.TraceVerbose($"Found valid resource: {resourceEntryFromDb.Name}");
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

        private static string FindOwner(string resourceName, string groupName, List<string> emails)
        {
            var owner = emails.FirstOrDefault(x => (resourceName + groupName).Contains(GetAlias(x)));
            return owner;
        }
        #endregion
    }
}
