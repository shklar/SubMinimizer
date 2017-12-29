using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using CogsMinimizer.Shared;

namespace CogsMinimizer
{
    public class MvcApplication : System.Web.HttpApplication
    {

        private Uri _prevUri = null;

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<DataAccess,
                CogsMinimizer.Migrations.Configuration>());
        }


        protected void Application_BeginRequest(object sender, EventArgs args)
        {
            if (Context.Request.Url.Scheme == "https")
            {
                return;
            }

            if (_prevUri != null && _prevUri.AbsoluteUri == Context.Request.Url.AbsoluteUri)
            {
                return;
            }

            // This is an insecure connection, so redirect to the secure version
            UriBuilder uri = new UriBuilder(Context.Request.Url);
            uri.Scheme = "https";

            if (uri.Host.Equals("localhost"))
            {
                uri.Port = 44335;
            }
            else
            {
                uri.Port = -1;
            }

            _prevUri = new Uri(uri.ToString());

            Response.Redirect(uri.ToString(), true);
        }
    }
}
