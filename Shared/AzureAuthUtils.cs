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
        public static AuthenticationResult AcquireUserToken(string organizationId)
        {
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => organizationId);


            // Get user name
            string signedInUserUniqueName = GetSignedInUserUniqueName();
            signedInUserUniqueName = "eviten@microsoft.com";

            // Aquire Access Token to call Azure Resource Manager
            string appClientId = AzureDataUtils.GetKeyVaultSecret("subminimizer", "appregid");
            string appPassword = AzureDataUtils.GetKeyVaultSecret("subminimizer", "appregpassword");
            ClientCredential credential = new ClientCredential(appClientId, appPassword);

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

        public static AuthenticationResult AcquireAppToken(string organizationId)
        {
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => organizationId);


            // Aquire App Only Access Token to call Azure Resource Manager - Client Credential OAuth Flow
            string appClientId = AzureDataUtils.GetKeyVaultSecret("subminimizer", "appregid");
            string appPassword = AzureDataUtils.GetKeyVaultSecret("subminimizer", "appregpassword");
            ClientCredential credential = new ClientCredential(appClientId, appPassword);

            // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
            AuthenticationContext authContext =
                new AuthenticationContext(string.Format(ConfigurationManager.AppSettings["ida:Authority"], organizationId));
            Task<AuthenticationResult> resultTask =
                authContext.AcquireTokenAsync(ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"],
                    credential);
            resultTask.Wait();

            return resultTask.Result;
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
