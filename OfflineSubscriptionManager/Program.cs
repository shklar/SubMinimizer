﻿using System;
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
            var host = new JobHost();

            Database.SetInitializer(new NullDatabaseInitializer<DataAccessModel>());
            // The following code will invoke a function called ManualTrigger and 
            // pass in data (value in this case) to the function
            host.Call(typeof(Functions).GetMethod("ManualTrigger"));
        }
    }
}
