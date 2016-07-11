using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;
using System;
using System.Configuration;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.Owin.Builder;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace CogsMinimizer
{
    public partial class Startup
    {
        private DataAccess db = new DataAccess();
        public void ConfigureAuth(IAppBuilder app)
        {
            System.Web.Helpers.AntiForgeryConfig.UniqueClaimTypeIdentifier = ClaimTypes.NameIdentifier;

            string ClientId = ConfigurationManager.AppSettings["ida:ClientID"];
            string Password = ConfigurationManager.AppSettings["ida:Password"];
            string Authority = string.Format(ConfigurationManager.AppSettings["ida:Authority"], "common");
            string GraphAPIIdentifier = ConfigurationManager.AppSettings["ida:GraphAPIIdentifier"];
            string AzureResourceManagerIdentifier = ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"];

            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);
            app.UseCookieAuthentication(new CookieAuthenticationOptions { });
            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    ClientId = ClientId,
                    Authority = Authority,
                    TokenValidationParameters = new System.IdentityModel.Tokens.TokenValidationParameters
                    {
                        // we inject our own multitenant validation logic
                        ValidateIssuer = false,
                    },
                    Notifications = new OpenIdConnectAuthenticationNotifications()
                    {
                        RedirectToIdentityProvider = (context) =>
                        {
                            // This ensures that the address used for sign in and sign out is picked up dynamically from the request
                            // this allows you to deploy your app (to Azure Web Sites, for example) without having to change settings
                            // Remember that the base URL of the address used here must be provisioned in Azure AD beforehand.
                            //string appBaseUrl = context.Request.Scheme + "://" + context.Request.Host + context.Request.PathBase;

                            object obj = null;
                            if (context.OwinContext.Environment.TryGetValue("Authority", out obj))
                            {
                                string authority = obj as string;
                                if (authority != null)
                                {
                                    context.ProtocolMessage.IssuerAddress = authority;
                                }
                            }
                            if (context.OwinContext.Environment.TryGetValue("DomainHint", out obj))
                            {
                                string domainHint = obj as string;
                                if (domainHint != null)
                                {
                                    context.ProtocolMessage.SetParameter("domain_hint", domainHint);
                                }
                            }
                            context.ProtocolMessage.RedirectUri = HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Path);
                            context.ProtocolMessage.PostLogoutRedirectUri = new UrlHelper(HttpContext.Current.Request.RequestContext).Action
                                ("Index", "Home", null, HttpContext.Current.Request.Url.Scheme);
                            //context.ProtocolMessage.Resource = GraphAPIIdentifier;
                            context.ProtocolMessage.Resource = AzureResourceManagerIdentifier;
                            return Task.FromResult(0);
                        },
                        AuthorizationCodeReceived = (context) =>
                        {
                            ClientCredential credential = new ClientCredential(ClientId, Password);
                            string tenantID = context.AuthenticationTicket.Identity.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
                            string signedInUserUniqueName = context.AuthenticationTicket.Identity.FindFirst(ClaimTypes.Name).Value.Split('#')[context.AuthenticationTicket.Identity.FindFirst(ClaimTypes.Name).Value.Split('#').Length - 1];

                            var tokenCache = new ADALTokenCache(signedInUserUniqueName);
                            tokenCache.Clear();

                            AuthenticationContext authContext = new AuthenticationContext(string.Format("https://login.windows.net/{0}", tenantID), tokenCache);

                            //var items = authContext.TokenCache.ReadItems().ToList();

                            AuthenticationResult result1 = authContext.AcquireTokenByAuthorizationCode(
                                context.Code, new Uri(HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Path)), credential);

                            //items = authContext.TokenCache.ReadItems().ToList();

                            //AuthenticationResult result2 = authContext.AcquireTokenSilent(ConfigurationManager.AppSettings["ida:AzureResourceManagerIdentifier"], credential,
                            //    new UserIdentifier(signedInUserUniqueName, UserIdentifierType.RequiredDisplayableId));

                            //items = authContext.TokenCache.ReadItems().ToList();

                            return Task.FromResult(0);
                        },
                        // we use this notification for injecting our custom logic
                        SecurityTokenValidated = (context) =>
                        {
                            // retriever caller data from the incoming principal
                            string issuer = context.AuthenticationTicket.Identity.FindFirst("iss").Value;
                            if (!issuer.StartsWith("https://sts.windows.net/"))
                                // the caller is not from a trusted issuer - throw to block the authentication flow
                                throw new System.IdentityModel.Tokens.SecurityTokenValidationException();

                            return Task.FromResult(0);
                        },
                        //AuthenticationFailed = (context) =>
                        //{
                        //    context.OwinContext.Response.Redirect(new UrlHelper(HttpContext.Current.Request.RequestContext).
                        //        Action("Index", "Home", null, HttpContext.Current.Request.Url.Scheme));
                        //    context.HandleResponse(); // Suppress the exception
                        //    return Task.FromResult(0);
                        //}
                    }
                });
        }
    }
}