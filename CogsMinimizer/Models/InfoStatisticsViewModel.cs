using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations.Model;
using System.Linq;
using System.Web;
using CogsMinimizer.Shared;

namespace CogsMinimizer.Models
{
    public class InfoStatisticsViewModel
    {
        public IEnumerable<AnalyzeRecord> AnalyzeRecords { get; set; }
    }
}