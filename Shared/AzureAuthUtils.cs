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
        /// <summary>
        ///  Authenticate application at AAD for access to given resource at given tenant
        /// </summary>
        /// <param name="organizationId">Organization ID</param>
        /// <param name="resourceId">Resource ID</param>
        /// <param name="tokenKind">Token kind application or user</param>
        /// <param name="silent">f true acquire token silently otherwise not</param>
        /// <returns>Token acquired</returns>
        public static AuthenticationResult Authenticate(string organizationId, string resourceId, TokenKind tokenKind, bool silent)
        {
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => organizationId);
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => resourceId);

            ClientCredential credential = AzureAuthUtils.GetAppClientCredential();
            Task<AuthenticationResult> resultTask = null;
            string authority = Settings.Instance.Authority;

            if (tokenKind == TokenKind.User)
            {

                // Get user name
                string signedInUserUniqueName = GetSignedInUserUniqueName();
                // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's EF DB
                AuthenticationContext authContext = new AuthenticationContext(
                    string.Format(authority, organizationId), new ADALTokenCache(signedInUserUniqueName));

                if (silent)
                {
                    resultTask = authContext.AcquireTokenSilentAsync(resourceId, credential, new UserIdentifier(signedInUserUniqueName, UserIdentifierType.RequiredDisplayableId));
                }
                else
                {
                    resultTask = authContext.AcquireTokenAsync(resourceId, credential);
                }
            }
            else
            {
                AuthenticationContext authContext =
                    new AuthenticationContext(string.Format(authority, organizationId));

                if (silent)
                {
                    resultTask = authContext.AcquireTokenSilentAsync(resourceId, credential, new UserIdentifier(Settings.Instance.AppClientId, UserIdentifierType.UniqueId));
                }
                else
                {
                    resultTask = authContext.AcquireTokenAsync(resourceId, credential);
                }
            }

            resultTask.Wait();
            return resultTask.Result;
        }

        /// <summary>
        ///  Acquire token for current user at given tenant
        /// </summary>
        /// <param name="organizationId">Organization ID</param>
        /// <returns>User token</returns>
        public static AuthenticationResult AcquireArmUserToken(string organizationId)
        {
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => organizationId);


            return Authenticate(organizationId, Settings.Instance.AzureResourceManagerIdentifier, TokenKind.User, true);
        }


        /// <summary>
        ///  Acquire token for application at given tenant
        /// </summary>
        /// <param name="organizationId">Organization ID</param>
        /// <returns>User token</returns>
        public static AuthenticationResult AcquireArmAppToken(string organizationId)
        {
            Diagnostics.EnsureStringNotNullOrWhiteSpace(() => organizationId);


            return Authenticate(organizationId, Settings.Instance.AzureResourceManagerIdentifier, TokenKind.Application, false);
        }


        /// <summary>
        /// Get app client Credential
        /// </summary>
        /// <returns>credential</returns>
        public static ClientCredential GetAppClientCredential()
        {
            string appClientId = Settings.Instance.AppClientId;
            string appPassword = Settings.Instance.AppPassword;
            return new ClientCredential(appClientId, appPassword);
        }

        /// <summary>
        ///  get current signed user
        /// </summary>
        /// <returns>Name</returns>
        public static string GetSignedInUserUniqueName()
        {
            string signedInUserUniqueName =
                ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#')[
                    ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value.Split('#').Length - 1];
            return signedInUserUniqueName;
        }

    }
}
