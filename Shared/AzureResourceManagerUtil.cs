using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Web.Helpers;
using CogsMinimizer.Shared;
using Microsoft.Azure.Management.Authorization;
using Microsoft.Azure.Management.Authorization.Models;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.ResourceManager.Models;
using Microsoft.Rest;
using Microsoft.Rest.Azure;
using ResourceManagementClient = Microsoft.Azure.Management.ResourceManager.ResourceManagementClient;
using Subscription = CogsMinimizer.Shared.Subscription;

namespace CogsMinimizer.Shared
{
    public static class AzureResourceManagerUtil
    {
        /// <summary>
        /// Dictionary contains api version to use for operations  with definite resource type which is dictionary key. 
        /// </summary>
        private static Dictionary<string, List<string>> resourceTypeApiVersionDictionary = new Dictionary<string, List<string>>();

        /// <summary>
        ///  Gets list of currently logged user organizations
        /// </summary>
        /// <returns>List of currently logged user organizations</returns>
        public static List<Organization> GetUserOrganizations()
        {
            List<Organization> organizations = new List<Organization>();
            string microsoftAADID = ConfigurationManager.AppSettings["ida:MicrosoftAADID"];

            string objectIdOfCloudSenseServicePrincipal =
                AzureADGraphAPIUtil.GetObjectIdOfServicePrincipalInOrganization(microsoftAADID,
                    ConfigurationManager.AppSettings["ida:ClientID"]);

            organizations.Add(new Organization()
            {
                Id = microsoftAADID,
                objectIdOfCloudSenseServicePrincipal = objectIdOfCloudSenseServicePrincipal
                //DisplayName = AzureADGraphAPIUtil.GetOrganizationDisplayName(organization.tenantId),
            });

            return organizations;
        }

        /// <summary>
        ///  Returns list of subscriptions for given organization (tenant) ID
        /// </summary>
        /// <param name="organizationId">Organization (tenant) ID</param>
        /// <returns>List of subscriptions</returns>
        public static List<Subscription> GetUserSubscriptions(string organizationId)
        {
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => organizationId);

            List<Subscription> subscriptions = null;


            try
            {
                AuthenticationResult result = AzureAuthUtils.AcquireUserToken(organizationId);

                subscriptions = new List<Subscription>();

                // Get subscriptions to which the user has some kind of access
                string requestUrl = string.Format("{0}/subscriptions?api-version={1}",
                    ConfigurationManager.AppSettings["ida:AzureResourceManagerUrl"],
                    ConfigurationManager.AppSettings["ida:AzureResourceManagerAPIVersion"]);

                // Make the GET request
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                HttpResponseMessage response = client.SendAsync(request).Result;

                // Endpoint returns JSON with an array of Subscription Objects
                // id                                                  subscriptionId                       displayName state
                // --                                                  --------------                       ----------- -----
                // /subscriptions/c276fc76-9cd4-44c9-99a7-4fd71546436e c276fc76-9cd4-44c9-99a7-4fd71546436e Production  Enabled
                // /subscriptions/e91d47c4-76f3-4271-a796-21b4ecfe3624 e91d47c4-76f3-4271-a796-21b4ecfe3624 Development Enabled
                
                // add unsuccessful response handling
                if (response.IsSuccessStatusCode)
                {
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    var subscriptionsResult = (Json.Decode(responseContent)).value;

                    foreach (var subscription in subscriptionsResult)
                        subscriptions.Add(new Subscription()
                        {
                            Id = subscription.subscriptionId,
                            DisplayName = subscription.displayName,
                            OrganizationId = organizationId,

                        });
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            return subscriptions;
        }

        /// <summary>
        /// Get Api version supported for operations with given resource
        /// </summary>
        /// <param name="resourceManagementClient">Resource management client</param>
        /// <param name="resource">resource</param>
        /// <param name="apiVersionList">Api version list</param> 
        /// <returns>Retrieval result</returns>
        public static ResourceOperationResult GetApiVersionsSupportedForResource(ResourceManagementClient resourceManagementClient, Resource resource, ref List<string> apiVersionList)
        {
            Diagnostics.EnsureArgumentNotNull(() => resourceManagementClient);
            Diagnostics.EnsureArgumentNotNull(() => resource);

            if (apiVersionList == null)
            {
                apiVersionList = new List<string>();
            }
            else
            {
                apiVersionList.Clear();
            }

            ResourceOperationResult result = new ResourceOperationResult();
            ProviderResourceType resourceType = null;

            // if cached return cached value otherwise get resource type with list of api versions
            if (resourceTypeApiVersionDictionary.ContainsKey(resource.Type))
            {
                apiVersionList.AddRange(resourceTypeApiVersionDictionary[resource.Type]);
            }
            else
            {
                ResourceOperationResult getResourceTypeResult = GetResourceType(resourceManagementClient, resource, ref resourceType);

                if (getResourceTypeResult.Result == ResourceOperationStatus.Succeeded)
                {
                    resourceTypeApiVersionDictionary[resource.Type] = new List<string>(resourceType.ApiVersions);
                    apiVersionList.AddRange(resourceTypeApiVersionDictionary[resource.Type]);
                }
                else
                {
                    result.Result = ResourceOperationStatus.Failed;
                    result.FailureReason = getResourceTypeResult.FailureReason;
                    result.Message = getResourceTypeResult.Message;
                }
            }

                return result;
        }

        /// <summary>
        ///  Check if user has access to subscription
        /// </summary>
        /// <param name="SubscriptionId">Subscription Id</param>
        /// <param name="organizationId">Organization Id</param>
        /// <return>True if user has access to specified subscription false otherwise</returns>
        public static bool UserCanManageAccessForSubscription(string subscriptionId, string organizationId)
        {
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => subscriptionId);
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => organizationId);

            bool ret = false;

            try
            {
                AuthenticationResult result = AzureAuthUtils.AcquireUserToken(organizationId);


                // Get permissions of the user on the subscription
                string requestUrl =
                    string.Format("{0}/subscriptions/{1}/providers/microsoft.authorization/permissions?api-version={2}",
                        ConfigurationManager.AppSettings["ida:AzureResourceManagerUrl"], subscriptionId,
                        ConfigurationManager.AppSettings["ida:ARMAuthorizationPermissionsAPIVersion"]);

                // Make the GET request
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                HttpResponseMessage response = client.SendAsync(request).Result;

                // Endpoint returns JSON with an array of Actions and NotActions
                // actions  notActions
                // -------  ----------
                // {*}      {Microsoft.Authorization/*/Write, Microsoft.Authorization/*/Delete}
                // {*/read} {}

                // add unsuccessful response handling
                if (response.IsSuccessStatusCode)
                {
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    var permissionsResult = (Json.Decode(responseContent)).value;

                    foreach (var permissions in permissionsResult)
                    {
                        bool permissionMatch = false;
                        foreach (string action in permissions.actions)
                        {
                            var actionPattern = "^" + Regex.Escape(action.ToLower()).Replace("\\*", ".*") + "$";
                            permissionMatch = Regex.IsMatch("microsoft.authorization/roleassignments/write",
                                actionPattern);
                            if (permissionMatch) break;
                        }
                        // if one of the actions match, check that the NotActions don't
                        if (permissionMatch)
                        {
                            foreach (string notAction in permissions.notActions)
                            {
                                var notActionPattern = "^" + Regex.Escape(notAction.ToLower()).Replace("\\*", ".*") +
                                                       "$";
                                if (Regex.IsMatch("microsoft.authorization/roleassignments/write", notActionPattern))
                                    permissionMatch = false;
                                if (!permissionMatch) break;
                            }
                        }
                        if (permissionMatch)
                        {
                            ret = true;
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            return ret;
        }


        /// <summary>
        /// Check if application has access to specified subscription
        /// </summary>
        /// <param name="SubscriptionId">Subscription Id</param>
        /// <param name="organizationId">Organization Id</param>
        /// <return>True if application has access to specified subscription false otherwise</returns>
        public static bool ServicePrincipalHasReadAccessToSubscription(string subscriptionId, string organizationId)
        {
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => subscriptionId);
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => organizationId);

            bool ret = false;

            try
            {
                AuthenticationResult result = AzureAuthUtils.AcquireAppToken(organizationId);

                // Get permissions of the app on the subscription
                string requestUrl =
                    string.Format("{0}/subscriptions/{1}/providers/microsoft.authorization/permissions?api-version={2}",
                        ConfigurationManager.AppSettings["ida:AzureResourceManagerUrl"], subscriptionId,
                        ConfigurationManager.AppSettings["ida:ARMAuthorizationPermissionsAPIVersion"]);

                // Make the GET request
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                HttpResponseMessage response = client.SendAsync(request).Result;

                // Endpoint returns JSON with an array of Actions and NotActions
                // actions  notActions
                // -------  ----------
                // {*}      {Microsoft.Authorization/*/Write, Microsoft.Authorization/*/Delete}
                // {*/read} {}

                // add unsuccessful response handling
                if (response.IsSuccessStatusCode)
                {
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    var permissionsResult = (Json.Decode(responseContent)).value;

                    foreach (var permissions in permissionsResult)
                    {
                        bool permissionMatch = false;
                        foreach (string action in permissions.actions)
                            if (action.Equals("*/read", StringComparison.CurrentCultureIgnoreCase) ||
                                action.Equals("*", StringComparison.CurrentCultureIgnoreCase))
                            {
                                permissionMatch = true;
                                break;
                            }
                        // if one of the actions match, check that the NotActions don't
                        if (permissionMatch)
                        {
                            foreach (string notAction in permissions.notActions)
                                if (notAction.Equals("*", StringComparison.CurrentCultureIgnoreCase) ||
                                    notAction.EndsWith("/read", StringComparison.CurrentCultureIgnoreCase))
                                {
                                    permissionMatch = false;
                                    break;
                                }
                        }
                        if (permissionMatch)
                        {
                            ret = true;
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            return ret;
        }

        /// <summary>
        /// Get needed azure resource management role for the specified access
        /// </summary>
        /// <param name="level">Access level</param>
        /// <returns>Needed role</returns>
        public static AzureResourceManagementRole GetNeededAzureResourceManagementRole(SubscriptionManagementLevel level)
        {
            if (level == SubscriptionManagementLevel.ReportOnly)
            {
                return AzureResourceManagementRole.Reader;
            }
            else
            {
                return AzureResourceManagementRole.Contributor;
            }
        }

        /// <summary>
        /// Grant role to service principal on subscription
        /// </summary>
        /// <param name="objectId">application ID</param>
        /// <param name="SubscriptionId">Subscription Id</param>
        /// <param name="organizationId">Organization Id</param>
        /// <param name="role">Role</param>
        public static void GrantRoleToServicePrincipalOnSubscription(string objectId, string subscriptionId,
            string organizationId, AzureResourceManagementRole role)
        {
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => objectId);
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => organizationId);
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => subscriptionId);

            try
            {
                AuthenticationResult result = AzureAuthUtils.AcquireUserToken(organizationId);

                // Create role assignment for application on the subscription
                string roleAssignmentId = Guid.NewGuid().ToString();
                string roleDefinitionId =
                    GetRoleId(role, subscriptionId,
                        organizationId);

                string requestUrl =
                    string.Format(
                        "{0}/subscriptions/{1}/providers/microsoft.authorization/roleassignments/{2}?api-version={3}",
                        ConfigurationManager.AppSettings["ida:AzureResourceManagerUrl"], subscriptionId,
                        roleAssignmentId,
                        ConfigurationManager.AppSettings["ida:ARMAuthorizationRoleAssignmentsAPIVersion"]);

                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Put, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                StringContent content =
                    new StringContent("{\"properties\": {\"roleDefinitionId\":\"" + roleDefinitionId +
                                      "\",\"principalId\":\"" + objectId + "\"}}");
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                request.Content = content;
                // add unsuccessful response handling
                HttpResponseMessage response = client.SendAsync(request).Result;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Revokes all the roles of the application for this subscription
        /// </summary>
        /// <param name="objectId"></param>
        /// <param name="subscriptionId"></param>
        /// <param name="organizationId"></param>
        public static void RevokeAllRolesFromServicePrincipalOnSubscription(string objectId, string subscriptionId, string organizationId)
        {
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => objectId);
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => subscriptionId);
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => organizationId);

            try
            {
                AuthenticationResult result = AzureAuthUtils.AcquireUserToken(organizationId);

                // Get rolesAssignments to application on the subscription
                string requestUrl =
                    string.Format(
                        "{0}/subscriptions/{1}/providers/microsoft.authorization/roleassignments?api-version={2}&$filter=principalId eq '{3}'",
                        ConfigurationManager.AppSettings["ida:AzureResourceManagerUrl"], subscriptionId,
                        ConfigurationManager.AppSettings["ida:ARMAuthorizationRoleAssignmentsAPIVersion"], objectId);

                // Make the GET request
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                HttpResponseMessage response = client.SendAsync(request).Result;

                // Endpoint returns JSON with an array of role assignments
                // properties                                  id                                          type                                        name
                // ----------                                  --                                          ----                                        ----
                // @{roleDefinitionId=/subscriptions/e91d47... /subscriptions/e91d47c4-76f3-4271-a796-2... Microsoft.Authorization/roleAssignments     9db2cdc1-2971-42fe-bd21-c7c4ead4b1b8

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    var roleAssignmentsResult = (Json.Decode(responseContent)).value;

                    //remove all role assignments
                    foreach (var roleAssignment in roleAssignmentsResult)
                    {
                        requestUrl = string.Format("{0}{1}?api-version={2}",
                            ConfigurationManager.AppSettings["ida:AzureResourceManagerUrl"], roleAssignment.id,
                            ConfigurationManager.AppSettings["ida:ARMAuthorizationRoleAssignmentsAPIVersion"]);
                        request = new HttpRequestMessage(HttpMethod.Delete, requestUrl);
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                        // add unsuccessful response handling
                        response = client.SendAsync(request).Result;
                    }
                }
                // add unsuccessful response handling
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Get resource type of specified resource
        /// </summary>
        /// <param name="resourceManagementClient">Resource management client</param>
        /// <param name="resource">Resource</param>
        /// <returns>Resource type</returns>
        public static ResourceOperationResult GetResourceType(ResourceManagementClient resourceManagementClient, Resource resource, ref ProviderResourceType resourceType)
        {
            Diagnostics.EnsureArgumentNotNull(() => resourceManagementClient);
            Diagnostics.EnsureArgumentNotNull(() => resource);

            ResourceOperationResult result = new ResourceOperationResult();
            resourceType = null;

            string[] dbResourceTypeParts = resource.Type.Split(new char[] { '/' });
            string dbResourceProvider = dbResourceTypeParts[0];
            string dbResourceType = dbResourceTypeParts[1];
            
            try
            {
                IEnumerable<Provider> providerList = resourceManagementClient.Providers.List();
                IEnumerable<ProviderResourceType> resourceTypes = from p in providerList where p.NamespaceProperty == dbResourceProvider from t in p.ResourceTypes where t.ResourceType == dbResourceType select t;
                if (resourceTypes.Count() > 0)
                {
                    resourceType = resourceTypes.First();
                }
                else
                {
                    result.Result = ResourceOperationStatus.Failed;
                    result.FailureReason = FailureReason.ResourceTypeNotFound;
                    result.Message = "Resource type isn't found";
                }
            }
            catch (Exception e)
            {
                result.Result = ResourceOperationStatus.Failed;
                result.FailureReason = FailureReason.UnknownError;
                result.Message = "Resource type retrieval error";
            }

            return result;
        }

        /// <summary>
        /// Get specified azure management role Id
        /// </summary>
        /// <param name="role">Role</param>
        /// <param name="subscriptionId">Subscription Id</param>
        /// <param name="organizationId">Organization Id</param>
        /// <returns>Specified role</returns>
        public static string GetRoleId(AzureResourceManagementRole role, string subscriptionId, string organizationId)
        {
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => subscriptionId);
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => organizationId);


            string roleId = null;
            string roleName = role.ToString();

            try
            {
                AuthenticationResult result = AzureAuthUtils.AcquireUserToken(organizationId);

                // Get subscriptions to which the user has some kind of access
                string requestUrl =
                    string.Format(
                        "{0}/subscriptions/{1}/providers/Microsoft.Authorization/roleDefinitions?api-version={2}",
                        ConfigurationManager.AppSettings["ida:AzureResourceManagerUrl"], subscriptionId,
                        ConfigurationManager.AppSettings["ida:ARMAuthorizationRoleDefinitionsAPIVersion"]);

                // Make the GET request
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                HttpResponseMessage response = client.SendAsync(request).Result;

                // Endpoint returns JSON with an array of roleDefinition Objects
                // properties                                  id                                          type                                        name
                // ----------                                  --                                          ----                                        ----
                // @{roleName=Contributor; type=BuiltInRole... /subscriptions/e91d47c4-76f3-4271-a796-2... Microsoft.Authorization/roleDefinitions     b24988ac-6180-42a0-ab88-20f7382dd24c
                // @{roleName=Owner; type=BuiltInRole; desc... /subscriptions/e91d47c4-76f3-4271-a796-2... Microsoft.Authorization/roleDefinitions     8e3af657-a8ff-443c-a75c-2fe8c4bcb635
                // @{roleName=Reader; type=BuiltInRole; des... /subscriptions/e91d47c4-76f3-4271-a796-2... Microsoft.Authorization/roleDefinitions     acdd72a7-3385-48ef-bd42-f606fba81ae7
                // ...
                
                // add unsuccessful response handling
                if (response.IsSuccessStatusCode)
                {
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    var roleDefinitionsResult = (Json.Decode(responseContent)).value;

                    foreach (var roleDefinition in roleDefinitionsResult)
                        if ((roleDefinition.properties.roleName as string).Equals(roleName,
                            StringComparison.CurrentCultureIgnoreCase))
                        {
                            roleId = roleDefinition.id;
                            break;
                        }
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            return roleId;
        }

        /// <summary>
        ///  Get resource list of specified group
        /// </summary>
        /// <param name="resourceClient">Resource Client</param>
        /// <param name="groupName">Group name</param>
        /// <returns>Resource list</returns>
        public static IEnumerable<GenericResource> GetResourceList(ResourceManagementClient resourceClient,
            string groupName)
        {
            Diagnostics.EnsureArgumentNotNull(() => resourceClient);
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => groupName);

            var resourceList = resourceClient.ResourceGroups.ListResources(groupName);
            return resourceList;
        }

        /// <summary>
        ///  Get resource groups
        /// </summary>
        /// <param name="resourceClient">Resource client</param>
        /// <returns>Resource group list</returns>
        public static IEnumerable<ResourceGroup> GetResourceGroups(ResourceManagementClient resourceClient)
        {
            Diagnostics.EnsureArgumentNotNull(() => resourceClient);

            var resourceGroupsList = resourceClient.ResourceGroups.List();
            return resourceGroupsList;
        }

        /// <summary>
        /// Get subscription admins
        /// </summary>
        /// <param name="authClient">Authorization management client</param>
        /// <returns>Administrators list</returns>
        public static IEnumerable<ClassicAdministrator> GetSubscriptionAdmins(
            AuthorizationManagementClient authClient)
        {
            Diagnostics.EnsureArgumentNotNull(() => authClient);

            var admins = authClient.ClassicAdministrators.List("2015-06-01");
            return admins;
        }

        #region Management Clients

        /// <summary>
        /// Get user resource management client
        /// </summary>
        /// <param name="SubscriptionId">Subscription Id</param>
        /// <param name="organizationId">Organization Id</param>
        /// <returns>Resource management client</returns>
        public static ResourceManagementClient GetUserResourceManagementClient(string subscriptionId, string organizationId)
        {
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => subscriptionId);
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => organizationId);

            AuthenticationResult result = AzureAuthUtils.AcquireUserToken(organizationId);

            var credentials = new TokenCredentials(result.AccessToken);
            var resourceClient = new ResourceManagementClient(credentials) {SubscriptionId = subscriptionId};
            return resourceClient;
        }

        /// <summary>
        /// Get  application resource management client
        /// </summary>
        /// <param name="SubscriptionId">Subscription Id</param>
        /// <param name="organizationId">Organization Id</param>
        /// <returns>Resource management client</returns>
        public static ResourceManagementClient GetAppResourceManagementClient(string subscriptionId, string organizationId)
        {
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => subscriptionId);
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => organizationId);

            AuthenticationResult result = AzureAuthUtils.AcquireAppToken(organizationId);

            var credentials = new TokenCredentials(result.AccessToken);
            var resourceClient = new ResourceManagementClient(credentials) { SubscriptionId = subscriptionId };
            return resourceClient;
        }

        /// <summary>
        /// Get user  authorization management client
        /// </summary>
        /// <param name="SubscriptionId">Subscription Id</param>
        /// <param name="organizationId">Organization Id</param>
        /// <returns>Authorization management client</returns>
        public static AuthorizationManagementClient GetUserAuthorizationManagementClient(string subscriptionId,
            string organizationId)
        {
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => subscriptionId);
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => organizationId);

            AuthenticationResult result = AzureAuthUtils.AcquireUserToken(organizationId);

            var credentials = new TokenCredentials(result.AccessToken);
            var authorizationManagementClient = new AuthorizationManagementClient(credentials)
            {
                SubscriptionId = subscriptionId
            };
            return authorizationManagementClient;
        }

        /// <summary>
        /// Get application authorization management client
        /// </summary>
        /// <param name="SubscriptionId">Subscription Id</param>
        /// <param name="organizationId">Organization Id</param>
        /// <returns>Authorization management client</returns>
        public static AuthorizationManagementClient GetAppAuthorizationManagementClient(string subscriptionId,
         string organizationId)
        {
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => subscriptionId);
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => organizationId);

            AuthenticationResult result = AzureAuthUtils.AcquireAppToken(organizationId);

            var credentials = new TokenCredentials(result.AccessToken);
            var authorizationManagementClient = new AuthorizationManagementClient(credentials)
            {
                SubscriptionId = subscriptionId
            };
            return authorizationManagementClient;
        }

        /// <summary>
        /// Get Azure resource
        /// </summary>
        /// <param name="resourceClient">Resource management client</param>
        /// <param name="azureresourceid">Azure resource ID</param>
        /// <param name="Event tracer</param>
        /// <returns>Resource retrieved</returns>
        public static GenericResource GetAzureResource(ResourceManagementClient resourceClient, string azureresourceid, string apiVersion, ITracer tracer)
        {
            Diagnostics.EnsureArgumentNotNull(() => resourceClient);
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => azureresourceid);
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => apiVersion);

            try
            {
                tracer.TraceVerbose($"Getting resource {azureresourceid} with API version: {apiVersion}");
                GenericResource resource = resourceClient.Resources.GetById(azureresourceid, apiVersion);
                tracer.TraceVerbose($"Succeeded getting resource {azureresourceid} with API version: {apiVersion}");
                return resource;
            }
            catch (Exception e)
            {
                // Resource isn't found or another error
                tracer.TraceError($"Failed to get the resource {azureresourceid} with API version: {apiVersion}, error: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Delete azure resource
        /// </summary>
        /// <param name="resourceClient">Resource management client</param>
        /// <param name="resource">resource</param>
        /// <param name="tracer">Event tracer</param>
        /// <returns>Deleting result</returns>
        public static ResourceOperationResult DeleteAzureResource(ResourceManagementClient resourceClient, Resource resource, ITracer tracer)
        {
            Diagnostics.EnsureArgumentNotNull(() => resource);
            Diagnostics.EnsureArgumentNotNull(() => resourceClient);
            Diagnostics.EnsureArgumentNotNull(() => tracer);

            List<string> apiVersionList = null;
            ResourceOperationResult result = new ResourceOperationResult();
            try
            {                
                tracer.TraceVerbose($"Trying to delete the resource {resource.AzureResourceIdentifier}");

                ResourceOperationResult getApiResult = GetApiVersionsSupportedForResource(resourceClient, resource, ref apiVersionList);
                if (getApiResult.Result == ResourceOperationStatus.Failed)
                {
                    CloudException exception = new CloudException("Couldn't find proper API version to delete resource");
                    exception.Response = new HttpResponseMessageWrapper(new HttpResponseMessage(), getApiResult.Message);
                    throw exception;
                }

                // Let's try delete resource by each of retrieved api versions
                foreach (string apiVersion in apiVersionList)
                {
                    try
                    {
                        resourceClient.Resources.DeleteById(resource.AzureResourceIdentifier, apiVersion);
                        tracer.TraceVerbose($"Deleted the resource {resource.AzureResourceIdentifier} with API version: {apiVersion}");
                        result.Result = ResourceOperationStatus.Succeeded;
                        result.FailureReason = FailureReason.NoError;
                        result.Message = string.Empty;
                        break;
                    }
                    catch (CloudException e)
                    {
                        // If all delete efforts failed we return fail with parameters of last fail exception
                        result.Result = ResourceOperationStatus.Failed;
                        result.FailureReason = GetFailureReason(e.Response.Content);
                        result.Message = e.Response.Content;

                        // If resource not found don't try another versions
                        if (result.FailureReason == FailureReason.ResourceNotFound)
                        {
                            break;
                        }
                    }
                }

            }
            catch (CloudException e)
            {                
                tracer.TraceError($"Failed to delete the resource {resource.AzureResourceIdentifier}, error: {e.Message},  response content: {e.Response.Content}");
                result.Result = ResourceOperationStatus.Failed;
                result.FailureReason = GetFailureReason(e.Response.Content);
                result.Message = e.Response.Content;
            }

            // If resource not found suppose it was deleted manually, return success result
            if (result.Result == ResourceOperationStatus.Failed && result.FailureReason == FailureReason.ResourceNotFound)
            {
                result.Result = ResourceOperationStatus.Succeeded;
                result.FailureReason = FailureReason.NoError;
                result.Message = string.Empty;
            }

            return result;
        }

        /// <summary>
        /// Get failure reason extracting it from diagnostic message
        /// </summary>
        /// <param name="content">Diagnostic message</param>
        /// <return>Failure reason</returns>
        public static FailureReason GetFailureReason(string content)
        {
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => content);

            if (Regex.IsMatch(content, "cannot be deleted because it is in use by the following resources"))
            {
                return FailureReason.ResourceInUse;
            }
            else if (Regex.IsMatch(content, "No registered resource provider found for location[\\w\\W]*and API version"))
            {
                return FailureReason.WrongApiVersion;
            }
            else if (Regex.IsMatch(content, "The Resource[\\w\\W]*under resource group[\\w\\W]*was not found."))
            {
                return FailureReason.ResourceNotFound;
            }
            else
            {
                return FailureReason.UnknownError;
            }
        }


        #endregion
    }
}