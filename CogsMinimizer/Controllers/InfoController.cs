using CogsMinimizer.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.IdentityModel.Claims;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CogsMinimizer.Shared;
using Microsoft.Azure.Management.Authorization.Models;
using Resource = CogsMinimizer.Shared.Resource;

namespace CogsMinimizer.Controllers
{
    [Authorize]
    public class InfoController : SubMinimizerController
    {
        private DataAccess db = new DataAccess();

        public ActionResult Statistics()
        {
            InfoStatisticsViewModel model = new InfoStatisticsViewModel();
            List<AnalyzeRecord> analyzeRecords = new List<AnalyzeRecord>();

            foreach (AnalyzeRecord analyzeRecord in db.AnalyzeRecords)
            {
                analyzeRecords.Add(analyzeRecord);
            }

            model.AnalyzeRecords = analyzeRecords;

            return View(model); 
        }

    }
}
