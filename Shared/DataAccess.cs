﻿using System.Data.Common;
using System.Data.Entity;

namespace CogsMinimizer.Shared
{
    public class DataAccess : DbContext
    {
        public DataAccess() : base(Settings.Instance.DataAccessConnectionString) {}
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<PerUserTokenCache> PerUserTokenCacheList { get; set; }
        public DbSet<Resource> Resources { get; set; }
    }
    public class DataAccessInitializer : System.Data.Entity.DropCreateDatabaseIfModelChanges<DataAccess>
    {

    }
}