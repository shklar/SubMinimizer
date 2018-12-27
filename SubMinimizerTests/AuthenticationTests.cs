using System;
using System.Configuration;
using System.Threading.Tasks;

using System.Net.Http.Headers;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Web.Helpers;
using System.Security.Claims;

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using CogsMinimizer.Shared;

namespace SubMinimizerTests
{
    [TestClass]
    public class AuthenticationTests
    {
        [TestMethod]
        public void TestGetAppToken()
        {
            //  test get token by call to our utility class
            // Test getting application token access to Azure resource manager as resource for resource manipulation

            // Let's check access to some subscription through request just for checking validity of token received
            // Get permissions of the app on the subscription

            string organizationId = ConfigurationManager.AppSettings["ida:MicrosoftAADID"];
            string appToken = AzureAuthUtils.AcquireAppToken(organizationId).AccessToken;

            string subscriptionId = "bcbd775a-813c-46e8-afe5-1a66912e0f03";
            string requestUrl =
                string.Format("{0}/subscriptions/{1}/providers/microsoft.authorization/permissions?api-version={2}",
                    ConfigurationManager.AppSettings["ida:AzureResourceManagerUrl"], subscriptionId,
                    ConfigurationManager.AppSettings["ida:ARMAuthorizationPermissionsAPIVersion"]);

            // Make the GET request
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", appToken);
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
                var permissionsResult = Json.Decode(responseContent).value;
            }
            else
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void TestAuthenticateSilent()
        {
            //  test get token by call to our utility class
            // Test getting application token access to Azure resource manager as resource for resource manipulation

            // Let's check access to some subscription through request just for checking validity of token received
            // Get permissions of the app on the subscription

            string organizationId = ConfigurationManager.AppSettings["ida:MicrosoftAADID"];
            string appToken = AzureAuthUtils.Authenticate(organizationId, ConfigurationManager.AppSettings["ida:AzureResourceManagerUrl"], true).AccessToken;

            string subscriptionId = "bcbd775a-813c-46e8-afe5-1a66912e0f03";
            string requestUrl =
                string.Format("{0}/subscriptions/{1}/providers/microsoft.authorization/permissions?api-version={2}",
                    ConfigurationManager.AppSettings["ida:AzureResourceManagerUrl"], subscriptionId,
                    ConfigurationManager.AppSettings["ida:ARMAuthorizationPermissionsAPIVersion"]);

            // Make the GET request
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", appToken);
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
                var permissionsResult = Json.Decode(responseContent).value;
            }
            else
            {
                Assert.Fail();
            }
        }


        [TestMethod]
        public void TestAuthenticate()
        {
            //  test get token by call to our utility class
            // Test getting application token access to Azure resource manager as resource for resource manipulation

            // Let's check access to some subscription through request just for checking validity of token received
            // Get permissions of the app on the subscription

            string organizationId = ConfigurationManager.AppSettings["ida:MicrosoftAADID"];
            string appToken = AzureAuthUtils.Authenticate(organizationId, ConfigurationManager.AppSettings["ida:AzureResourceManagerUrl"], false).AccessToken;

            string subscriptionId = "bcbd775a-813c-46e8-afe5-1a66912e0f03";
            string requestUrl =
                string.Format("{0}/subscriptions/{1}/providers/microsoft.authorization/permissions?api-version={2}",
                    ConfigurationManager.AppSettings["ida:AzureResourceManagerUrl"], subscriptionId,
                    ConfigurationManager.AppSettings["ida:ARMAuthorizationPermissionsAPIVersion"]);

            // Make the GET request
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", appToken);
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
                var permissionsResult = Json.Decode(responseContent).value;
            }
            else
            {
                Assert.Fail();
            }
        }


        [TestMethod]
        public void TestGetAppTokenAdal()
        {
            //  test get token by direct call to ADAL

            string appToken = GetAppTokenAdal();

            // Let's check access to some subscription through request just for checking validity of token received
            // Get permissions of the app on the subscription

            string subscriptionId = "bcbd775a-813c-46e8-afe5-1a66912e0f03";
            string requestUrl =
                string.Format("{0}/subscriptions/{1}/providers/microsoft.authorization/permissions?api-version={2}",
                    ConfigurationManager.AppSettings["ida:AzureResourceManagerUrl"], subscriptionId,
                    ConfigurationManager.AppSettings["ida:ARMAuthorizationPermissionsAPIVersion"]);

            // Make the GET request
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", appToken);
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
                var permissionsResult = Json.Decode(responseContent).value;
            }
            else
            {
                Assert.Fail();
            }

        }
        
        [TestMethod]
        public void TestGetTokenSilentAdal()
        {
            //  test get token silently by direct call to ADAL
            string organizationId = ConfigurationManager.AppSettings["ida:MicrosoftAADID"];
            string userToken = GetAppTokenSilentAdal(organizationId).AccessToken;

            // Check token received
            // Received token for resource  manager access
            // Get subscriptions to which the user has some kind of access
            string requestUrl = string.Format("{0}/subscriptions?api-version={1}",
                ConfigurationManager.AppSettings["ida:AzureResourceManagerUrl"],
                ConfigurationManager.AppSettings["ida:AzureResourceManagerAPIVersion"]);

            // Make the GET request
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
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

            }
            else
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void TestGetTokenSilent()
        {
            // test get token silent implementation  at our utility class
            string organizationId = ConfigurationManager.AppSettings["ida:MicrosoftAADID"];
            string userToken = AzureAuthUtils.AcquireUserToken(organizationId).AccessToken;

            // Check token received
            // Received token for resource  manager access
            // Get subscriptions to which the user has some kind of access
            string requestUrl = string.Format("{0}/subscriptions?api-version={1}",
                ConfigurationManager.AppSettings["ida:AzureResourceManagerUrl"],
                ConfigurationManager.AppSettings["ida:AzureResourceManagerAPIVersion"]);

            // Make the GET request
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
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

            }
            else
            {
                Assert.Fail();
            }
        }

        [TestMethod]
        public void TestGetKeyVaultSecret()
        {
            string value = AzureDataUtils.GetKeyVaultSecret("subminimizer", "testsecret");
            Assert.AreEqual("Testcontent", value);
        }

        private AuthenticationResult GetAppTokenSilentAdal(string organizationId)
        {
            // Get user name
            string signedInUserUniqueName = AzureAuthUtils.GetSignedInUserUniqueName();
            signedInUserUniqueName = "eviten@microsoft.com";
            // Aquire Access Token to call Azure Resource Manager
            ClientCredential credential = AzureAuthUtils.GetAppClientCredential();

            // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
            AuthenticationContext authContext = new AuthenticationContext(
                string.Format(ConfigurationManager.AppSettings["ida:Authority"], organizationId),
                new ADALTokenCache(signedInUserUniqueName));
            Task<AuthenticationResult> resultTask =
                authContext.AcquireTokenSilentAsync(ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"],
                    credential,
                    new UserIdentifier(signedInUserUniqueName, UserIdentifierType.RequiredDisplayableId));
            resultTask.Wait();
            AuthenticationResult result = resultTask.Result;
            return result;
        }

        private string GetAppTokenAdal()
        {
            // test getting application token access to Azure resource manager as resource for resource manipulation
            // use application credentials for getting token

            // Aquire App Only Access Token to call Azure Resource Manager - Client Credential OAuth Flow
            string organizationId = ConfigurationManager.AppSettings["ida:MicrosoftAADID"];

            ClientCredential credential = AzureAuthUtils.GetAppClientCredential();
        
            // Initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
            AuthenticationContext authContext =
                new AuthenticationContext(string.Format(ConfigurationManager.AppSettings["ida:Authority"], organizationId));
            Task<AuthenticationResult> resultTask =
                authContext.AcquireTokenAsync(ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"],
                    credential);
            resultTask.Wait();

            return resultTask.Result.AccessToken;
        }
    }
}
