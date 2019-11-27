using System;
using System.Collections.Generic;
using System.Configuration;
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
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<DataAccess,
                CogsMinimizer.Migrations.Configuration>());

            Microsoft.ApplicationInsights.Extensibility.TelemetryConfiguration.Active.InstrumentationKey = ConfigurationManager.AppSettings["APPINSIGHTS_INSTRUMENTATIONKEY"];
        }
    }
}
