using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace CogsMinimizer.Shared
{
    public static class AzureAuthUtils
    {
        public static AuthenticationResult AcquireUserToken(string organizationId)
        {
            // Get user name
            string signedInUserUniqueName = GetSignedInUserUniqueName();

            // Aquire Access Token to call Azure Resource Manager
            ClientCredential credential = new ClientCredential(ConfigurationManager.AppSettings["ida:ClientID"],
                ConfigurationManager.AppSettings["ida:Password"]);
            // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
            AuthenticationContext authContext = new AuthenticationContext(
                string.Format(ConfigurationManager.AppSettings["ida:Authority"], organizationId),
                new ADALTokenCache(signedInUserUniqueName));
            AuthenticationResult result =
                authContext.AcquireTokenSilent(ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"],
                    credential,
                    new UserIdentifier(signedInUserUniqueName, UserIdentifierType.RequiredDisplayableId));
            return result;
        }

        public static AuthenticationResult AcquireAppToken(string organizationId)
        {
            // Aquire App Only Access Token to call Azure Resource Manager - Client Credential OAuth Flow
            ClientCredential credential = new ClientCredential(ConfigurationManager.AppSettings["ida:ClientID"],
                ConfigurationManager.AppSettings["ida:Password"]);
            // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
            AuthenticationContext authContext =
                new AuthenticationContext(string.Format(ConfigurationManager.AppSettings["ida:Authority"], organizationId));
            AuthenticationResult result =
                authContext.AcquireToken(ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"],
                    credential);
            return result;
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
