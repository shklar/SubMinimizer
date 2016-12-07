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

            string tenantId =
                ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
            var signedInUserUniqueName = AzureAuthUtils.GetSignedInUserUniqueName();

            try
            {
                // Aquire Access Token to call Azure Resource Manager
                ClientCredential credential = new ClientCredential(ConfigurationManager.AppSettings["ida:ClientID"],
                    ConfigurationManager.AppSettings["ida:Password"]);
                // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
                AuthenticationContext authContext = new AuthenticationContext(
                    string.Format(ConfigurationManager.AppSettings["ida:Authority"], tenantId),
                    new ADALTokenCache(signedInUserUniqueName));
                AuthenticationResult result =
                    authContext.AcquireTokenSilent(
                        ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"], credential,
                        new UserIdentifier(signedInUserUniqueName, UserIdentifierType.RequiredDisplayableId));



                // Get a list of Organizations of which the user is a member            
                string requestUrl = string.Format("{0}/tenants?api-version={1}",
                    ConfigurationManager.AppSettings["ida:AzureResourceManagerUrl"],
                    ConfigurationManager.AppSettings["ida:AzureResourceManagerAPIVersion"]);

                // Make the GET request
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                HttpResponseMessage response = client.SendAsync(request).Result;

                // Endpoint returns JSON with an array of Tenant Objects
                // id                                            tenantId
                // --                                            --------
                // /tenants/7fe877e6-a150-4992-bbfe-f517e304dfa0 7fe877e6-a150-4992-bbfe-f517e304dfa0
                // /tenants/62e173e9-301e-423e-bcd4-29121ec1aa24 62e173e9-301e-423e-bcd4-29121ec1aa24

                // add unsuccessful response handling
                if (response.IsSuccessStatusCode)
                {
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    var organizationsResult = (Json.Decode(responseContent)).value;

                    foreach (var organization in organizationsResult)
                        organizations.Add(new Organization()
                        {
                            Id = organization.tenantId,
                            //DisplayName = AzureADGraphAPIUtil.GetOrganizationDisplayName(organization.tenantId),
                            objectIdOfCloudSenseServicePrincipal =
                                AzureADGraphAPIUtil.GetObjectIdOfServicePrincipalInOrganization(organization.tenantId,
                                    ConfigurationManager.AppSettings["ida:ClientID"])
                        });
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            return organizations;
        }

        public static List<Subscription> GetUserSubscriptions(string organizationId)
        {
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
            var resourceList = resourceClient.ResourceGroups.ListResources(groupName);
            return resourceList;
        }


        public static IEnumerable<ResourceGroup> GetResourceGroups(ResourceManagementClient resourceClient)
        {     
            var resourceGroupsList = resourceClient.ResourceGroups.List();
            return resourceGroupsList;
        }

        public static IEnumerable<ClassicAdministrator> GetSubscriptionAdmins(
            AuthorizationManagementClient authClient)
        {
            var admins = authClient.ClassicAdministrators.List("2015-06-01");
            return admins;
        }

        #region Management Clients

        public static ResourceManagementClient GetUserResourceManagementClient(string subscriptionId, string organizationId)
        {
            AuthenticationResult result = AzureAuthUtils.AcquireUserToken(organizationId);

            var credentials = new TokenCredentials(result.AccessToken);
            var resourceClient = new ResourceManagementClient(credentials) {SubscriptionId = subscriptionId};
            return resourceClient;
        }

        public static ResourceManagementClient GetAppResourceManagementClient(string subscriptionId, string organizationId)
        {
            AuthenticationResult result = AzureAuthUtils.AcquireAppToken(organizationId);

            var credentials = new TokenCredentials(result.AccessToken);
            var resourceClient = new ResourceManagementClient(credentials) { SubscriptionId = subscriptionId };
            return resourceClient;
        }

        public static AuthorizationManagementClient GetUserAuthorizationManagementClient(string subscriptionId,
            string organizationId)
        {
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
            string [] apiVersion = { "2015-01-01", "2014-04-01", "2015-08-01" , "2016-05-01", "2016-01-01", "2016-04-01",
                "2016-09-01", "2015-11-01", "2015-03-20", "2015-03-01-preview" };

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
                        throw e;
                    }
                }
            }
        }

     
    }
}