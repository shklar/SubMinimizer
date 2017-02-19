using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Web.Helpers;
using CogsMinimizer.Shared;
using Microsoft.Azure.Management.Authorization;
using Microsoft.Azure.Management.Authorization.Models;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.ResourceManager.Models;
using Microsoft.Rest;
using ResourceManagementClient = Microsoft.Azure.Management.ResourceManager.ResourceManagementClient;
using Subscription = CogsMinimizer.Shared.Subscription;

namespace CogsMinimizer.Shared
{
    public static class AzureResourceManagerUtil
    {
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

        public static IEnumerable<GenericResource> GetResourceList(ResourceManagementClient resourceClient,
            string groupName)
        {
            Diagnostics.EnsureArgumentNotNull(() => resourceClient);
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => groupName);

            var resourceList = resourceClient.ResourceGroups.ListResources(groupName);
            return resourceList;
        }


        public static IEnumerable<ResourceGroup> GetResourceGroups(ResourceManagementClient resourceClient)
        {
            Diagnostics.EnsureArgumentNotNull(() => resourceClient);

            var resourceGroupsList = resourceClient.ResourceGroups.List();
            return resourceGroupsList;
        }

        public static IEnumerable<ClassicAdministrator> GetSubscriptionAdmins(
            AuthorizationManagementClient authClient)
        {
            Diagnostics.EnsureArgumentNotNull(() => authClient);

            var admins = authClient.ClassicAdministrators.List("2015-06-01");
            return admins;
        }

        #region Management Clients

        public static ResourceManagementClient GetUserResourceManagementClient(string subscriptionId, string organizationId)
        {
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => subscriptionId);
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => organizationId);

            AuthenticationResult result = AzureAuthUtils.AcquireUserToken(organizationId);

            var credentials = new TokenCredentials(result.AccessToken);
            var resourceClient = new ResourceManagementClient(credentials) {SubscriptionId = subscriptionId};
            return resourceClient;
        }

        public static ResourceManagementClient GetAppResourceManagementClient(string subscriptionId, string organizationId)
        {
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => subscriptionId);
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => organizationId);

            AuthenticationResult result = AzureAuthUtils.AcquireAppToken(organizationId);

            var credentials = new TokenCredentials(result.AccessToken);
            var resourceClient = new ResourceManagementClient(credentials) { SubscriptionId = subscriptionId };
            return resourceClient;
        }

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

        #endregion

        public static void DeleteAzureResource(ResourceManagementClient resourceClient, string azureresourceid, ITracer tracer)
        {
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => azureresourceid);
            Diagnostics.EnsureArgumentNotNull(() => resourceClient);
            Diagnostics.EnsureArgumentNotNull(() => tracer);

            string[] apiVersion = { "2015-01-01", "2014-04-01", "2015-08-01" , "2016-05-01", "2016-01-01", "2016-04-01",
                "2016-09-01", "2015-11-01", "2015-03-20", "2015-03-01-preview", "2015-08-01-preview",
                "2016-12-01", "2017-03-30", "2015-10-31", "2015-03-15" ,"2014-02-26", "2016-03-30", "2015-11-01-preview",
                "2016-10-01" };

            for (int i = 0; i < apiVersion.Length; i++)
            {
                try
                {
                    tracer.TraceVerbose($"Trying to delete the resource {azureresourceid} with API version: {apiVersion[i]}");
                    resourceClient.Resources.DeleteById(azureresourceid, apiVersion[i]);

                    //If successfully deleted the resource no need to continue
                    tracer.TraceVerbose($"Deleted the resource {azureresourceid} with API version: {apiVersion[i]}");
                    return;
                }
                catch (Exception e)
                {
                    tracer.TraceError($"Failed to delete the resource {azureresourceid} with API version: {apiVersion[i]}");
                    if (i==apiVersion.Length-1)
                    {
                        if (!string.IsNullOrEmpty(e.Message))
                        {
                            tracer.TraceError($"Failed to delete the resource {azureresourceid} with error message {e.Message}");
                        }
                        if (!string.IsNullOrEmpty(e.InnerException?.Message))
                        {
                            tracer.TraceError($"Failed to delete the resource {azureresourceid} with internal exception message {e.InnerException.Message}");
                        }

                        throw;
                    }
                }
            }
        }

     
    }
}