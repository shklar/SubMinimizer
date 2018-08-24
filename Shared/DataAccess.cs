using System.Data.Common;
using System.Data.Entity;

using CogsMinimizer.Migrations;

namespace CogsMinimizer.Shared
{
    public class DataAccess : DbContext
    {
        public DataAccess() : base("DataAccess") { }
        public DbSet<Subscription> Subscriptions { get; set;}
        public DbSet<PerUserTokenCache> PerUserTokenCacheList { get; set; }
        public DbSet<Resource> Resources { get; set; }
        public DbSet<AnalyzeRecord> AnalyzeRecords { get; set; }
    }    
    public class DataAccessInitializer : System.Data.Entity.MigrateDatabaseToLatestVersion<DataAccess, Configuration>
    {
    }
}