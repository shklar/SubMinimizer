using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace CogsMinimizer.Shared
{
    public static class AzureAuthUtils
    {
        public static AuthenticationResult Authenticate(string organizationId, string resourceId, bool silent)
        {
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => organizationId);
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => resourceId);

            ClientCredential credential = AzureAuthUtils.GetAppClientCredential();

            Task<AuthenticationResult> resultTask = null;
            if (silent)
            {
                // Get user name
                string signedInUserUniqueName = GetSignedInUserUniqueName();
                signedInUserUniqueName = "eviten@microsoft.com";

                // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
                AuthenticationContext authContext = new AuthenticationContext(
                    string.Format(ConfigurationManager.AppSettings["ida:Authority"], organizationId), new ADALTokenCache(signedInUserUniqueName));

                resultTask =
                    authContext.AcquireTokenSilentAsync(resourceId, credential, new UserIdentifier(signedInUserUniqueName, UserIdentifierType.RequiredDisplayableId));
            }
            else
            {
                AuthenticationContext authContext =
                    new AuthenticationContext(string.Format(ConfigurationManager.AppSettings["ida:Authority"], organizationId));
                resultTask =
                    authContext.AcquireTokenAsync(resourceId, credential);
            }

            resultTask.Wait();
            return resultTask.Result;
        }

        public static AuthenticationResult AcquireUserToken(string organizationId)
        {
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => organizationId);


            return Authenticate(organizationId, ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"], true);
        }

        public static AuthenticationResult AcquireAppToken(string organizationId)
        {
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => organizationId);


            return Authenticate(organizationId, ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"], false);
        }


        public static ClientCredential GetAppClientCredential()
        {
            string appClientId = Settings.Instance.AppClientId;
            string appPassword = Settings.Instance.AppPassword;
            return new ClientCredential(appClientId, appPassword);
        }

        public static string GetSignedInUserUniqueName()
        {
            string signedInUserUniqueName =
                ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#')[
                    ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#').Length - 1];
            return signedInUserUniqueName;
        }

    }
}
