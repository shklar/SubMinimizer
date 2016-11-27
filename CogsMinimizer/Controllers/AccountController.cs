using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Configuration;
using System.Web;
using System.Web.Mvc;

namespace CogsMinimizer.Controllers
{
    public class AccountController : SubMinimizerController
    {
        // sign in triggered from the Sign In View
        // configured to return to the home page upon successful authentication
        public void SignIn(string directoryName = "common")
        {
            if (!Request.IsAuthenticated)
            {
                // note configuration (keys, etc…) will not necessarily understand this authority.
                HttpContext.GetOwinContext().Environment.Add("Authority", string.Format(ConfigurationManager.AppSettings["ida:Authority"] + "OAuth2/Authorize", directoryName));
                HttpContext.GetOwinContext().Authentication.Challenge(new AuthenticationProperties { RedirectUri = this.Url.Action("Index", "Home") }, OpenIdConnectAuthenticationDefaults.AuthenticationType);
            }
        }

        // sign out triggered from the Sign Out gesture in the UI
        // after sign out, it redirects to Post_Logout_Redirect_Uri (as set in Startup.Auth.cs)
        public void SignOut()
        {
            HttpContext.GetOwinContext().Authentication.SignOut(
                OpenIdConnectAuthenticationDefaults.AuthenticationType, CookieAuthenticationDefaults.AuthenticationType);
        }
    }
}