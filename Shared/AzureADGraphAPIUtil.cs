using System;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Helpers;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

using CogsMinimizer.Shared;

namespace CogsMinimizer
{
    public static class AzureADGraphAPIUtil
    {
        public static string GetOrganizationDisplayName(string organizationId)
        {
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => organizationId);

            string displayName = null;
            try
            {
                AuthenticationResult result = AzureAuthUtils.Authenticate(organizationId, Settings.Instance.GetSetting("ida:GraphAPIIdentifier"), TokenKind.User, true);

                // Get a list of Organizations of which the user is a member
                string requestUrl = string.Format("{0}{1}/tenantDetails?api-version={2}", Settings.Instance.GetSetting("ida:GraphAPIIdentifier"),
                    organizationId, Settings.Instance.GetSetting("ida:GraphAPIVersion"));

                // Make the GET request
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                HttpResponseMessage response = client.SendAsync(request).Result;

                // Endpoint returns JSON with an array of Tenant Objects
                // add unsuccessful response handling
                if (response.IsSuccessStatusCode)
                {
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    var organizationPropertiesResult = (Json.Decode(responseContent)).value;
                    if (organizationPropertiesResult != null && organizationPropertiesResult.Length > 0)
                    {
                        displayName = organizationPropertiesResult[0].displayName;
                        if (organizationPropertiesResult[0].verifiedDomains != null)
                            foreach (var verifiedDomain in organizationPropertiesResult[0].verifiedDomains)
                                if (verifiedDomain["default"])
                                    displayName += " (" + verifiedDomain.name + ")";
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            return displayName;
        }
        public static string GetObjectIdOfServicePrincipalInOrganization(string organizationId, string applicationId)
        {
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => organizationId);
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => applicationId);

            string objectId = null;

            try
            {

                AuthenticationResult result = AzureAuthUtils.Authenticate(organizationId, Settings.Instance.GetSetting("ida:GraphAPIIdentifier"), TokenKind.Application, false);
                
                // Get a list of Organizations of which the user is a member
                string requestUrl = string.Format("{0}{1}/servicePrincipals?api-version={2}&$filter=appId eq '{3}'",
                    Settings.Instance.GetSetting("ida:GraphAPIIdentifier"), organizationId, Settings.Instance.GetSetting("ida:GraphAPIVersion"), applicationId);

                // Make the GET request
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
                HttpResponseMessage response = client.SendAsync(request).Result;

                // Endpoint should return JSON with one or none serviePrincipal object
                // add unsuccessful response handling
                if (response.IsSuccessStatusCode)
                {
                    string responseContent = response.Content.ReadAsStringAsync().Result;
                    var servicePrincipalResult = (Json.Decode(responseContent)).value;
                    if (servicePrincipalResult != null && servicePrincipalResult.Length > 0)
                        objectId = servicePrincipalResult[0].objectId;
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            return objectId;
        }
        public static string LookupDisplayNameOfAADObject(string organizationId, string objectId)
        {
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => organizationId);
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => objectId);

            string objectDisplayName = null;

            // Aquire Access Token to call Azure AD Graph API
            AuthenticationResult result = AzureAuthUtils.Authenticate(organizationId, Settings.Instance.GetSetting("ida:GraphAPIIdentifier"), TokenKind.User, true);

            HttpClient client = new HttpClient();

            string doQueryUrl = string.Format("{0}{1}/directoryObjects/{2}?api-version={3}",
                Settings.Instance.GetSetting("ida:GraphAPIIdentifier"), organizationId,
                objectId, Settings.Instance.GetSetting("ida:GraphAPIVersion"));

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, doQueryUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            HttpResponseMessage response = client.SendAsync(request).Result;

            // add unsuccessful response handling
            if (response.IsSuccessStatusCode)
            {
                var responseContent = response.Content;
                string responseString = responseContent.ReadAsStringAsync().Result;
                var directoryObject = System.Web.Helpers.Json.Decode(responseString);
                if (directoryObject != null) objectDisplayName = string.Format("{0} ({1})", directoryObject.displayName, directoryObject.objectType);
            }

            return objectDisplayName;
        }
    }
}