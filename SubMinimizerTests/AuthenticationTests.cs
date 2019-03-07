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
using Microsoft.Azure;
using Microsoft.Azure.Management.Authorization;

namespace SubMinimizerTests
{
    [TestClass]
    public class AuthenticationTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            if (!Settings.Initialize())
            {
                throw new ApplicationException("Failed initialize settings.");
            }
        }

        [TestMethod]
        public void TestAccessArmWithAppToken()
        {
            //  test get token by call to our utility class
            // Test getting application token access to Azure resource manager as resource for resource manipulation

            // Let's check access to some subscription through request just for checking validity of token received
            // Get permissions of the app on the subscription

            string organizationId = Settings.Instance.GetSetting("ida:MicrosoftAADID");
            string appToken = AzureAuthUtils.AcquireArmAppToken(organizationId).AccessToken;

            string subscriptionId = "bcbd775a-813c-46e8-afe5-1a66912e0f03";
            string requestUrl =
                string.Format("{0}/subscriptions/{1}/providers/microsoft.authorization/permissions?api-version={2}",
                    Settings.Instance.GetSetting("ida:AzureResourceManagerUrl"), subscriptionId,
                    Settings.Instance.GetSetting("ida:ARMAuthorizationPermissionsAPIVersion"));

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
        public void TestArmAccessWithUserToken()
        {
            //  test get token by call to our utility class
            // Test getting application token access to Azure resource manager as resource for resource manipulation

            // Let's check access to some subscription through request just for checking validity of token received
            // Get permissions of the app on the subscription

            string organizationId = Settings.Instance.GetSetting("ida:MicrosoftAADID");
            string userToken = AzureAuthUtils.Authenticate(organizationId, Settings.Instance.GetSetting("ida:AzureResourceManagerUrl"), TokenKind.User, true).AccessToken;

            string subscriptionId = "bcbd775a-813c-46e8-afe5-1a66912e0f03";
            string requestUrl =
                string.Format("{0}/subscriptions/{1}/providers/microsoft.authorization/permissions?api-version={2}",
                    Settings.Instance.GetSetting("ida:AzureResourceManagerUrl"), subscriptionId,
                    Settings.Instance.GetSetting("ida:ARMAuthorizationPermissionsAPIVersion"));

            // Make the GET request
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
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
        public void TestAccessArbitraryResourceWithAppToken()
        {
            //  test get token by call to our utility class
            // Test getting application token access to Azure resource manager as resource for resource manipulation

            // Let's check access to some subscription through request just for checking validity of token received
            // Get permissions of the app on the subscription

            string organizationId = Settings.Instance.GetSetting("ida:MicrosoftAADID");
            string appToken = AzureAuthUtils.Authenticate(organizationId, Settings.Instance.GetSetting("ida:AzureResourceManagerUrl"), TokenKind.Application, false).AccessToken;

            string subscriptionId = "bcbd775a-813c-46e8-afe5-1a66912e0f03";
            string requestUrl =
                string.Format("{0}/subscriptions/{1}/providers/microsoft.authorization/permissions?api-version={2}",
                    Settings.Instance.GetSetting("ida:AzureResourceManagerUrl"), subscriptionId,
                    Settings.Instance.GetSetting("ida:ARMAuthorizationPermissionsAPIVersion"));

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
        public void TestArmAccessWithAdalDirectCallAppToken()
        {
            //  test get token by direct call to ADAL

            string appToken = GetAppTokenAdal();

            // Let's check access to some subscription through request just for checking validity of token received
            // Get permissions of the app on the subscription

            string subscriptionId = "bcbd775a-813c-46e8-afe5-1a66912e0f03";
            string requestUrl =
                string.Format("{0}/subscriptions/{1}/providers/microsoft.authorization/permissions?api-version={2}",
                    Settings.Instance.GetSetting("ida:AzureResourceManagerUrl"), subscriptionId,
                    Settings.Instance.GetSetting("ida:ARMAuthorizationPermissionsAPIVersion"));

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
        public void TestArmAccessWithAdalDirectCallUserToken()
        {
            //  test get token silently by direct call to ADAL
            string organizationId = Settings.Instance.GetSetting("ida:MicrosoftAADID");
            string userToken = GetAppTokenSilentAdal(organizationId).AccessToken;

            // Check token received
            // Received token for resource  manager access
            // Get subscriptions to which the user has some kind of access
            string requestUrl = string.Format("{0}/subscriptions?api-version={1}",
                Settings.Instance.GetSetting("ida:AzureResourceManagerUrl"),
                Settings.Instance.GetSetting("ida:AzureResourceManagerAPIVersion"));

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
        public void TestAcquireUserTokenMethod()
        {
            // test get token silent implementation  at our utility class
            string organizationId = Settings.Instance.GetSetting("ida:MicrosoftAADID");
            string userToken = AzureAuthUtils.AcquireArmUserToken(organizationId).AccessToken;

            // Check token received
            // Received token for resource  manager access
            // Get subscriptions to which the user has some kind of access
            string requestUrl = string.Format("{0}/subscriptions?api-version={1}",
                Settings.Instance.GetSetting("ida:AzureResourceManagerUrl"),
                 Settings.Instance.GetSetting("ida:AzureResourceManagerAPIVersion"));

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
            string value = Utilities.GetKeyVaultSecret("subminimizer", "testsecret");
            Assert.AreEqual("Testcontent", value);
        }

        [TestMethod]
        public void TestGetAuthorizationManagementClient()
        {
            // Get subscriptions to which the user has some kind of access
            string subscriptionId = "f168ad75-c916-40ee-8d26-fa5344d0a101";
            string subscriptionUri =
                string.Format(
                    "{0}/subscriptions/{1}",
                    Settings.Instance.GetSetting("ida:AzureResourceManagerUrl"), subscriptionId);

            string organizationId = Settings.Instance.GetSetting("ida:MicrosoftAADID");
            string appToken = AzureAuthUtils.AcquireArmAppToken(organizationId).AccessToken;
            var credentials = new TokenCloudCredentials(appToken);

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("api-version", "2018-11-01");

//            var a = AzureResourceManagerUtil.GetSubscriptionAdmins2(subscriptionId, organizationId);
            AuthorizationManagementClient authorizationManagementClient = new AuthorizationManagementClient(credentials, new Uri(subscriptionUri), client);
            var admins = authorizationManagementClient.ClassicAdministrators.List();
        }

        private AuthenticationResult GetAppTokenSilentAdal(string organizationId)
        {
            // Get user name
            string signedInUserUniqueName = AzureAuthUtils.GetSignedInUserUniqueName();
            // Aquire Access Token to call Azure Resource Manager
            ClientCredential credential = AzureAuthUtils.GetAppClientCredential();

            // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
            AuthenticationContext authContext = new AuthenticationContext(
                string.Format(Settings.Instance.GetSetting("ida:Authority"), organizationId),
                new ADALTokenCache(signedInUserUniqueName));
            Task<AuthenticationResult> resultTask =
                authContext.AcquireTokenSilentAsync(Settings.Instance.GetSetting("ida:AzureResourceManagerIdentifier"),
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

            // Aquire App Only Access Token to call Azure Revsource Manager - Client Credential OAuth Flow
            string organizationId = Settings.Instance.GetSetting("ida:MicrosoftAADID");

            ClientCredential credential = AzureAuthUtils.GetAppClientCredential();
        
            // Initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
            AuthenticationContext authContext =
                new AuthenticationContext(string.Format(Settings.Instance.GetSetting("ida:Authority"), organizationId));
            Task<AuthenticationResult> resultTask =
                authContext.AcquireTokenAsync(Settings.Instance.GetSetting("ida:AzureResourceManagerIdentifier"),
                    credential);
            resultTask.Wait();

            return resultTask.Result.AccessToken;
        }
    }
}
