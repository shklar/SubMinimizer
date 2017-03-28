using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using CogsMinimizer.Shared;

namespace SubMinimizerTests
{
    [TestClass]
    public class SubMinimizerTests
    {

        private Subscription CreateSubscription()
        {
            Subscription subscription = new Subscription();
            subscription.Id = Guid.NewGuid().ToString();
            subscription.OrganizationId = Guid.NewGuid().ToString();
            subscription.DisplayName = "subscription - " + subscription.Id;
            subscription.ExpirationIntervalInDays = 20;
            subscription.LastAnalysisDate = DateTime.UtcNow;
            subscription.ExpirationUnclaimedIntervalInDays = 10;
            subscription.ReserveIntervalInDays = 100;
            subscription.ManagementLevel = SubscriptionManagementLevel.ManualDelete;

            return subscription;
        }

        private Resource CreateResource()
        {
            Resource resource = new Resource();
            resource.Id = Guid.NewGuid().ToString();
            resource.AzureResourceIdentifier = Guid.NewGuid().ToString();
            resource.ResourceGroup = "group"; 
            resource.Type = "type";
            resource.Name = "resource - " + resource.Id;
            resource.Description = resource.Name + "  description";
            resource.FirstFoundDate = DateTime.UtcNow;
            resource.Owner = "owner@microsoft.com";
            resource.ConfirmedOwner = false;
            resource.ExpirationDate = DateTime.UtcNow;

            return resource;
        }


        public SubscriptionAnalysisResult CreateSubscriptionAnalysisResult()
        {
            // Let's create resource and subscription to test
            Subscription subscription = CreateSubscription();

            Random rnd = new Random();
            SubscriptionAnalysisResult result = new SubscriptionAnalysisResult(subscription);

            // Fill resources list simulating subscription analysis
         
            for (int resNum = 0; resNum < rnd.Next(20); resNum++)
            {
                Resource resource = CreateResource();
                // resources will be of three types in order check sort by type then by name
                resource.Type += ((int)resNum/3).ToString();
                resource.SubscriptionId = subscription.Id;
                resource.ExpirationDate = DateTime.UtcNow.AddDays(rnd.Next(100));
                result.DeletedResources.Add(resource);
            }


            for (int resNum = 0; resNum < rnd.Next(20); resNum++)
            {
                Resource resource = CreateResource();
                // resources will be of three types in order check sort by type then by name
                resource.Type += ((int)resNum / 3).ToString();
                resource.SubscriptionId = subscription.Id;
                resource.ExpirationDate = DateTime.UtcNow.AddDays(rnd.Next(100));
                result.FailedDeleteResources.Add(resource);
            }

            for (int resNum = 0; resNum < rnd.Next(20); resNum++)
            {
                Resource resource = CreateResource();
                // resources will be of three types in order check sort by type then by name
                resource.Type += ((int)resNum / 3).ToString();
                resource.SubscriptionId = subscription.Id;
                resource.ExpirationDate = DateTime.UtcNow.AddDays(rnd.Next(100));
                result.ExpiredResources.Add(resource);
            }

            for (int resNum = 0; resNum < rnd.Next(20); resNum++)
            {
                Resource resource = CreateResource();
                // resources will be of three types in order check sort by type then by name
                resource.Type += ((int)resNum / 3).ToString();
                resource.SubscriptionId = subscription.Id;
                resource.ExpirationDate = DateTime.UtcNow.AddDays(rnd.Next(100));
                result.NearExpiredResources.Add(resource);
            }

            for (int resNum = 0; resNum < rnd.Next(20); resNum++)
            {
                Resource resource = CreateResource();
                // resources will be of three types in order check sort by type then by name
                resource.Type += ((int)resNum / 3).ToString();
                resource.SubscriptionId = subscription.Id;
                resource.ExpirationDate = DateTime.UtcNow.AddDays(rnd.Next(100));
                result.NewResources.Add(resource);
            }

            for (int resNum = 0; resNum < rnd.Next(20); resNum++)
            {
                Resource resource = CreateResource();
                resource.SubscriptionId = subscription.Id;
                // resources will be of three types in order check sort by type then by name
                resource.Type += ((int)resNum / 3).ToString();
                resource.ExpirationDate = DateTime.UtcNow.AddDays(rnd.Next(100));
                result.NotFoundResources.Add(resource);
            }

            for (int resNum = 0; resNum < rnd.Next(20); resNum++)
            {
                Resource resource = CreateResource();
                // resources will be of three types in order check sort by type then by name
                resource.Type += ((int)resNum / 3).ToString();
                resource.SubscriptionId = subscription.Id;
                resource.ExpirationDate = DateTime.UtcNow.AddDays(rnd.Next(100));
                result.ValidResources.Add(resource);
            }

            result.AnalysisStartTime = DateTime.UtcNow;
            result.AnalysisEndTime = DateTime.UtcNow;

            return result;
        }


        [TestMethod]
        public void TestCreateEMail()
        {
            SubscriptionAnalysisResult result = CreateSubscriptionAnalysisResult();

            string emailTxt = EmailUtils.CreateEmailMessagePS(result, result.AnalyzedSubscription);
            
            // Test always succeeds but we can visually inspect here created mail with html visualizer
        }

        [TestMethod]
        public void TestIsExpiredResource()
        {
            // Let's create resource and subscription to test
            Resource resource = CreateResource();
            resource.ConfirmedOwner = true;
            Subscription subscription = CreateSubscription();
            resource.SubscriptionId = subscription.Id;

            // Make resource expired
            resource.ExpirationDate = DateTime.UtcNow;
            resource.ExpirationDate = resource.ExpirationDate.Subtract(new TimeSpan(2, 0, 0, 0));

            // Expect resource be expired
            Assert.IsTrue(ResourceOperationsUtil.HasExpired(resource));
        }

        [TestMethod]
        public void TestGetExpirationDate()
        {
            // Let's create resource and subscription to test
            Resource resource = CreateResource();
            resource.ConfirmedOwner = true;
            Subscription subscription = CreateSubscription();
            resource.SubscriptionId = subscription.Id;

            DateTime newExpirationDate = ResourceOperationsUtil.GetNewExpirationDate(subscription, resource);
            
            // Expect received expiration date greater than current date
            // Expect received expiration date difference with current data is about to established by subscription properties claimed resources expiration interval
            Assert.IsTrue(newExpirationDate > DateTime.UtcNow);
            Assert.IsTrue(Math.Abs(newExpirationDate.Subtract(DateTime.UtcNow).Days - subscription.ExpirationIntervalInDays) < 2);
        }

        [TestMethod]
        public void TestGetExpirationDateForUnclaimedResource()
        {
            // Let's create resource and subscription to test
            Resource resource = CreateResource();
            Subscription subscription = CreateSubscription();
            resource.SubscriptionId = subscription.Id;

            DateTime newExpirationDate = ResourceOperationsUtil.GetNewExpirationDate(subscription, resource);

            // Expect received expiration date greater than current date
            // Expect received expiration date difference with current data is about to established by subscription properties for unclaimed resources expiration interval
            Assert.IsTrue(newExpirationDate > DateTime.UtcNow);
            Assert.IsTrue(Math.Abs(newExpirationDate.Subtract(DateTime.UtcNow).Days - subscription.ExpirationUnclaimedIntervalInDays) < 2);
        }

        [TestMethod]
        public void TestGetReservationDate()
        {
            // Let's create resource and subscription to test
            Resource resource = CreateResource();
            resource.ConfirmedOwner = true;
            Subscription subscription = CreateSubscription();
            resource.SubscriptionId = subscription.Id;

            DateTime newExpirationDate = ResourceOperationsUtil.GetNewReserveDate(subscription, resource);

            // Expect received expiration date greater than current date
            // Expect received expiration date difference with current data is about to established by subscription properties for unclaimed resources expiration interval
            Assert.IsTrue(newExpirationDate > DateTime.UtcNow);
            Assert.IsTrue(Math.Abs(newExpirationDate.Subtract(DateTime.UtcNow).Days - subscription.ReserveIntervalInDays) < 2);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "Wrong subscription specified wasn't discovered.")]
        public void TestResetResourceFromWrongSubscription()
        {
            // Let's create resource and subscription to test
            Resource resource = CreateResource();
            resource.ConfirmedOwner = true;
            Subscription subscription = CreateSubscription();


            // Resource belong to another subscription
            resource.SubscriptionId = Guid.NewGuid().ToString();

            DateTime preResetExpirationDate = DateTime.UtcNow.Add(new TimeSpan(730, 0, 0, 0, 0));
            resource.ConfirmedOwner = true;
            resource.Status = ResourceStatus.Expired;
            resource.ExpirationDate = preResetExpirationDate;

            // Expect exception
            ResourceOperationsUtil.ResetResource(resource, subscription);
        }

        [TestMethod]
        public void TestResetResourceHappyFlow()
        {
            // Let's create resource and subscription to test
            Resource resource = CreateResource();
            resource.ConfirmedOwner = true;
            Subscription subscription = CreateSubscription();
            resource.SubscriptionId = subscription.Id;

            DateTime preResetExpirationDate = DateTime.UtcNow.Add(new TimeSpan(730, 0, 0, 0, 0));
            resource.ConfirmedOwner = true;
            resource.Status = ResourceStatus.Expired;
            resource.ExpirationDate = preResetExpirationDate;

            ResourceOperationsUtil.ResetResource(resource, subscription);

            // Resource properties were changed
            Assert.IsFalse(resource.ConfirmedOwner);
            Assert.AreEqual(ResourceStatus.Valid, resource.Status);

            // Expect received expiration date greater than current date
            // Expect received expiration date difference with current data is about to established by subscription properties for unclaimed resources expiration interval
            Assert.IsTrue(resource.ExpirationDate > DateTime.UtcNow);
            Assert.IsTrue(Math.Abs(resource.ExpirationDate.Subtract(DateTime.UtcNow).Days - subscription.ExpirationUnclaimedIntervalInDays) < 2);
        }
    }
}

