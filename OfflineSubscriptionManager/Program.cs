using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CogsMinimizer.Shared;
using Microsoft.Azure.WebJobs;

namespace OfflineSubscriptionManager
{
    // To learn more about Microsoft Azure WebJobs SDK, please see http://go.microsoft.com/fwlink/?LinkID=320976
    class Program
    {
        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        static void Main()
        {
            var cfg = new JobHostConfiguration();

            if (cfg.IsDevelopment)
            {
                cfg.UseDevelopmentSettings();
            }
            else
            {
                cfg.DashboardConnectionString = Settings.Instance.WebJobDashboardConnectionString;
                cfg.StorageConnectionString = Settings.Instance.WebJobStorageConnectionString;
            }

            var host = new JobHost(cfg);

            Database.SetInitializer(new MigrateDatabaseToLatestVersion<DataAccess, CogsMinimizer.Migrations.Configuration>());

            // The following code will invoke a function called ManualTrigger and 
            // pass in data (value in this case) to the function
            host.Call(typeof(Functions).GetMethod("ManualTrigger"));
        }
    }
}
