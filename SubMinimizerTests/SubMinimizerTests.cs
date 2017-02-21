using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.SQLite;
using System.Data.SQLite.EF6;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using CogsMinimizer.Shared;

namespace SubMinimizerTests
{
    [TestClass]
    public class SubMinimizerTests
    {

        private void FillSubMinimizerDatabase(DataAccess db)
        {
            // Add 10 subscriptions each having 10 resources
            for (int subscriptionNum = 0; subscriptionNum < 10; subscriptionNum++)
            {
                Subscription subscription = new Subscription();
                db.Subscriptions.Add(subscription);
                subscription.Id = Guid.NewGuid().ToString();
                subscription.DisplayName = "subscription - " + subscription.Id;
                subscription.ExpirationIntervalInDays = 20;
                subscription.ExpirationUnclaimedIntervalInDays = 10;
                subscription.ReserveIntervalInDays = 100;
                for (int resourceNum = 0; resourceNum < 10; resourceNum++)
                {
                    Resource resource = new Resource();
                    db.Resources.Add(resource);
                    resource.Id = Guid.NewGuid().ToString();
                    resource.SubscriptionId = subscription.Id;
                    resource.Name = "resource - " + subscription.Id;
                    resource.FirstFoundDate = DateTime.Now;
                    resource.ExpirationDate = DateTime.Now;
                }
            }
        }

        private DataAccess CreateSubMinimizerInMemoryDatabase()
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<DataAccess,
                CogsMinimizer.Migrations.Configuration>());

            DbConnection connection = SQLiteProviderFactory.Instance.CreateConnection();
            connection.ConnectionString = "Data Source=:memory:;";

            DataAccess db = new DataAccess(connection, true);
            db.Database.Initialize(true);

            return db;
        }

        private DataAccess CreateSubMinimizerDatabase()
        {
            DbProviderFactory providerFactory = DbProviderFactories.GetFactory("System.Data.SqlClient");
            string connectionString = ConfigurationManager.ConnectionStrings["DataAccess"].ConnectionString;
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            }

            DbConnection connection = providerFactory.CreateConnection();
            connection.ConnectionString = connectionString;
            connection.Open();

            return new DataAccess(connection, true);
        }

        [TestMethod]
        public void TestCollectionsLocalLoad()
        {
            DataAccess db = CreateSubMinimizerDatabase();

            // In tests we can not meanwhile create fully functional SubMinimizer database.
            // Sqlite we use for database provider for in memory database doesn't create tables automatically
            // Without tables we only can change and check local collections 
            // Since any operations about dbset (query, add, update) require tables
                        
            // Meanwhile  all production procedures that will have tests will operate local collections.
            // Code calling this procedures must load collections before calling procedures by DbSet.Load()

            // Meanwhile expect local collections empty
            Assert.AreEqual(0, db.Subscriptions.Local.Count);
            Assert.AreEqual(0, db.Resources.Local.Count);

            db.Subscriptions.Load();
            db.Resources.Load();

            // Expect Local collections are loaded now
            Assert.AreNotEqual(0, db.Subscriptions.Local.Count);
            Assert.AreNotEqual(0, db.Resources.Local.Count);
        }

        [TestMethod]
        public void TestSqlDatabase()
        {
            DbProviderFactory providerFactory = DbProviderFactories.GetFactory("System.Data.SqlClient");
            string connectionString = ConfigurationManager.ConnectionStrings["DataAccess"].ConnectionString;
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            }

            DbConnection connection = providerFactory.CreateConnection();
            connection.ConnectionString = connectionString;
            connection.Open();

            SqlCommand command = (SqlCommand)providerFactory.CreateCommand();
            command.CommandText = "select * from resources";
            command.Connection = (SqlConnection)connection;

            List<object> resultList = new List<object>();
            SqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                resultList.Add(reader[0]);
            }

            Assert.IsTrue(resultList.Count > 0);
        }

        [TestMethod]
        public void TestSqliteInMemoryDatabaseCreate()
        {
            SQLiteConnection connection = new SQLiteConnection("Data Source=:memory:;");
            using (connection)
            {
                connection.Open();
                connection.CreateModule(new SQLiteModuleEnumerable("sampleModule", new string[] { "one", "two", "three" }));
                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText = "CREATE VIRTUAL TABLE t1 USING sampleModule;";
                    command.ExecuteNonQuery();
                }

                using (SQLiteCommand command = connection.CreateCommand())
                {
                    command.CommandText = "select * from t1";

                    List<object> resultList = new List<object>();
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            resultList.Add(reader[0]);
                        }
                    }

                    Assert.AreEqual(3, resultList.Count);
                    Assert.AreEqual("one", resultList[0]);
                    Assert.AreEqual("two", resultList[1]);
                    Assert.AreEqual("three", resultList[2]);
                }
            }
        }

        [TestMethod]
        public void TestSubMinimizerDatabaseConnectivity()
        {
            // Check creation database with data from server. requires application database server connectivity.
            DataAccess db2 = CreateSubMinimizerDatabase();
            // Expect database be not empty
            Assert.AreNotEqual(0, db2.Resources.ToListAsync().Result);
            Assert.AreNotEqual(0, db2.Subscriptions.ToListAsync().Result);
        }


        [TestMethod]
        public void TestSubMinimizerInMemoryDatabaseUsing()
        {
            DataAccess db = CreateSubMinimizerInMemoryDatabase();

            // Expect database be empty. don't try load database sets since Sqlite actually doesn't create tables so we will get exception on load
            // Operate only with containing in collections instances
            Assert.AreEqual(0, db.Resources.Local.Count);
            Assert.AreEqual(0, db.Subscriptions.Local.Count);

            // Add some objects to database
            Resource resource = new Resource();
            resource.Id = Guid.NewGuid().ToString();
            db.Resources.Add(resource);
            Subscription subscription = new Subscription();
            subscription.Id = Guid.NewGuid().ToString();
            db.Subscriptions.Add(subscription);

            // Expect database be populated with objects we created
            Assert.AreEqual(1, db.Resources.Local.Count);
            Assert.AreEqual(1, db.Subscriptions.Local.Count);
        }

        [TestMethod]
        public void TestSubMinimizerInMemoryDatabaseUsingSideToSide()
        {
            // Initially acquire database from Sql server ensure it isn't empty.
            DataAccess dbSrv = CreateSubMinimizerDatabase();
            // Expect database be not empty
            Assert.AreNotEqual(0, dbSrv.Resources.ToListAsync().Result.Count);
            Assert.AreNotEqual(0, dbSrv.Subscriptions.ToListAsync().Result.Count);
            int resourceCount = dbSrv.Resources.Local.Count;
            int subscriptionCount = dbSrv.Subscriptions.Local.Count;

            DataAccess dbMemory = CreateSubMinimizerInMemoryDatabase();
            // Expect database be empty. don't try load database sets since Sqlite actually doesn't create tables so we will get exception on load
            // Operate only with containing in collections instances
            Assert.AreEqual(0, dbMemory.Resources.Local.Count);
            Assert.AreEqual(0, dbMemory.Subscriptions.Local.Count);

            // Add some objects to database
            Resource resource = new Resource();
            resource.Id = Guid.NewGuid().ToString();
            dbMemory.Resources.Add(resource);
            Subscription subscription = new Subscription();
            subscription.Id = Guid.NewGuid().ToString();
            dbMemory.Subscriptions.Add(subscription);

            // Expect database be populated with objects we created
            Assert.AreEqual(1, dbMemory.Resources.Local.Count);
            Assert.AreEqual(1, dbMemory.Subscriptions.Local.Count);

            // Expect no aside effect of changing database in Sql server database
            Assert.AreEqual(resourceCount, dbSrv.Resources.ToListAsync().Result.Count);
            Assert.AreEqual(subscriptionCount, dbSrv.Subscriptions.ToListAsync().Result.Count);
        }

        [TestMethod]
        public void TestIsExpiredResource()
        {
            // Create SubMinimizer database in memory
            DataAccess db = CreateSubMinimizerInMemoryDatabase();
            FillSubMinimizerDatabase(db);

            // Let's check first resource among created
            Resource resource = db.Resources.Local[0];
            resource.ExpirationDate = resource.ExpirationDate.Subtract(new TimeSpan(2, 0, 0, 0));

            // Expect resource be expired
            Assert.IsTrue(ResourceOperationsUtil.HasExpired(resource));
        }

        [TestMethod]
        public void TestGetExpirationDate()
        {
            // Create SubMinimizer database in memory
            DataAccess db = CreateSubMinimizerInMemoryDatabase();
            FillSubMinimizerDatabase(db);

            // Let's check first resource among created
            Resource resource = db.Resources.Local[0];
            resource.ConfirmedOwner = true;
            Subscription subscription = db.Subscriptions.Local[0];

            DateTime newExpirationDate = ResourceOperationsUtil.GetNewExpirationDate(subscription, resource);
            
            // Expect received expiration date greater than current date
            // Expect received expiration date difference with current data is about to established by subscription properties claimed resources expiration interval
            Assert.IsTrue(newExpirationDate > DateTime.Now);
            Assert.IsTrue(Math.Abs(newExpirationDate.Subtract(DateTime.Now).Days - subscription.ExpirationIntervalInDays) < 2);
        }

        [TestMethod]
        public void TestGetExpirationDateForUnclaimedResource()
        {
            // Create SubMinimizer database in memory
            DataAccess db = CreateSubMinimizerInMemoryDatabase();
            FillSubMinimizerDatabase(db);

            // Let's check first resource among created
            Resource resource = db.Resources.Local[0];
            resource.ConfirmedOwner = false;
            Subscription subscription = db.Subscriptions.Local[0];

            DateTime newExpirationDate = ResourceOperationsUtil.GetNewExpirationDate(subscription, resource);

            // Expect received expiration date greater than current date
            // Expect received expiration date difference with current data is about to established by subscription properties for unclaimed resources expiration interval
            Assert.IsTrue(newExpirationDate > DateTime.Now);
            Assert.IsTrue(Math.Abs(newExpirationDate.Subtract(DateTime.Now).Days - subscription.ExpirationUnclaimedIntervalInDays) < 2);
        }

        [TestMethod]
        public void TestGetReservationDate()
        {
            // Create SubMinimizer database in memory
            DataAccess db = CreateSubMinimizerInMemoryDatabase();
            FillSubMinimizerDatabase(db);

            // Let's check first resource among created
            Resource resource = db.Resources.Local[0];
            resource.ConfirmedOwner = false;
            Subscription subscription = db.Subscriptions.Local[0];
        
            DateTime newExpirationDate = ResourceOperationsUtil.GetNewReserveDate(subscription, resource);

            // Expect received expiration date greater than current date
            // Expect received expiration date difference with current data is about to established by subscription properties for unclaimed resources expiration interval
            Assert.IsTrue(newExpirationDate > DateTime.Now);
            Assert.IsTrue(Math.Abs(newExpirationDate.Subtract(DateTime.Now).Days - subscription.ReserveIntervalInDays) < 2);
        }

        [TestMethod]
        public void TestResetResourcesFromListFromSomeSubscriptions()
        {
            // Create SubMinimizer database in memory
            DataAccess db = CreateSubMinimizerInMemoryDatabase();
            FillSubMinimizerDatabase(db);

            // Let's store all current resources expiration dates.
            Dictionary<string, DateTime> resourceExpirationDictionary = new Dictionary<string, DateTime>();

            // After resetting resources we'll check that only resources of selected subscription changed the expression date
            Random rnd = new Random();
            int subscriptionNumber = rnd.Next(db.Subscriptions.Local.Count);
            Subscription subscriptionToReset = db.Subscriptions.Local[subscriptionNumber];
            DateTime preResetExpirationDate = DateTime.Now.Add(new TimeSpan(730, 0, 0, 0, 0));

            foreach (Resource resource in db.Resources.Local)
            {
                // Make all resources claimed, expired, set their expiration date
                // in order to check those fields are changed only for selected subscription
                resource.ConfirmedOwner = true;
                resource.Status = ResourceStatus.Expired;
                resource.ExpirationDate = preResetExpirationDate;

                resourceExpirationDictionary[resource.Id] = resource.ExpirationDate;
            }

            List<Resource> allResourcesList = db.Resources.Local.ToList();
            ResourceOperationsUtil.ResetResources(allResourcesList, subscriptionToReset);

            // Let's check after resetting resources only resources of selected subscription changed in expiration date and confirmed user fields
            foreach (Resource resource in db.Resources.Local)
            {
                if (resource.SubscriptionId == subscriptionToReset.Id)
                {
                    // Resources of selected subscription were changed
                    Assert.IsFalse(resource.ConfirmedOwner);
                    Assert.AreEqual(ResourceStatus.Valid, resource.Status);
                    Assert.AreNotEqual(resourceExpirationDictionary[resource.Id], resource.ExpirationDate);
                }
                else
                {
                    // Resources of remaining subscriptions weren't changed
                    Assert.IsTrue(resource.ConfirmedOwner);
                    Assert.AreEqual(ResourceStatus.Expired, resource.Status);
                    Assert.AreEqual(resourceExpirationDictionary[resource.Id], resource.ExpirationDate);
                }
            }
        }

        [TestMethod]
        public void TestResetResources()
        {
            // Create SubMinimizer database in memory
            DataAccess db = CreateSubMinimizerInMemoryDatabase();
            FillSubMinimizerDatabase(db);

            // Let's store all current resources expiration dates.
            Dictionary<string, DateTime> resourceExpirationDictionary = new Dictionary<string, DateTime>();

            // After resetting resources we'll check that only resources of selected subscription changed the expression date
            Random rnd = new Random();
            int subscriptionNumber = rnd.Next(db.Subscriptions.Local.Count);
            Subscription subscriptionToReset = db.Subscriptions.Local[subscriptionNumber];
            DateTime preResetExpirationDate = DateTime.Now.Add(new TimeSpan(730, 0, 0, 0, 0));

            foreach (Resource resource in db.Resources.Local)
            {
                // Make all resources claimed, expired, set their expiration date
                // in order to check those fields are changed only for selected subscription
                resource.ConfirmedOwner = true;
                resource.Status = ResourceStatus.Expired;
                resource.ExpirationDate = preResetExpirationDate;

                resourceExpirationDictionary[resource.Id] = resource.ExpirationDate;
            }

            List<Resource>  subscriptionResourcesList = db.Resources.Local.Where(r => r.SubscriptionId == subscriptionToReset.Id).ToList();

            ResourceOperationsUtil.ResetResources(subscriptionResourcesList, subscriptionToReset);

            // Let's check after resetting resources only resources of selected subscription changed in expiration date and confirmed user fields
            foreach (Resource resource in db.Resources.Local)
            {
                if (resource.SubscriptionId == subscriptionToReset.Id)
                {
                    // Resources of selected subscription were changed
                    Assert.IsFalse(resource.ConfirmedOwner);
                    Assert.AreEqual(ResourceStatus.Valid, resource.Status);
                    Assert.AreNotEqual(resourceExpirationDictionary[resource.Id], resource.ExpirationDate);
                }
                else
                {
                    // Resources of remaining subscriptions weren't changed
                    Assert.IsTrue(resource.ConfirmedOwner);
                    Assert.AreEqual(ResourceStatus.Expired, resource.Status);
                    Assert.AreEqual(resourceExpirationDictionary[resource.Id], resource.ExpirationDate);
                }
            }
        }
    }
}

