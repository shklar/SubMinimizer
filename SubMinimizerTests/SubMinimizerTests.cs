using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
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

        private Subscription CreateSubscription()
        {
            Subscription subscription = new Subscription();
            subscription.Id = Guid.NewGuid().ToString();
            subscription.DisplayName = "subscription - " + subscription.Id;
            subscription.ExpirationIntervalInDays = 20;
            subscription.ExpirationUnclaimedIntervalInDays = 10;
            subscription.ReserveIntervalInDays = 100;
            return subscription;
        }

        private Resource CreateResource()
        {
            Resource resource = new Resource();
            resource.Id = Guid.NewGuid().ToString();
            resource.Name = "resource - " + resource.Id;
            resource.FirstFoundDate = DateTime.Now;
            resource.ExpirationDate = DateTime.Now;
            return resource;
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
            Assert.IsTrue(newExpirationDate > DateTime.Now);
            Assert.IsTrue(Math.Abs(newExpirationDate.Subtract(DateTime.Now).Days - subscription.ExpirationIntervalInDays) < 2);
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
            Assert.IsTrue(newExpirationDate > DateTime.Now);
            Assert.IsTrue(Math.Abs(newExpirationDate.Subtract(DateTime.Now).Days - subscription.ExpirationUnclaimedIntervalInDays) < 2);
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
            Assert.IsTrue(newExpirationDate > DateTime.Now);
            Assert.IsTrue(Math.Abs(newExpirationDate.Subtract(DateTime.Now).Days - subscription.ReserveIntervalInDays) < 2);
        }

        [TestMethod]
        public void TestResetResourceFromWrongSubscription()
        {
            // Let's create resource and subscription to test
            Resource resource = CreateResource();
            resource.ConfirmedOwner = true;
            Subscription subscription = CreateSubscription();


            // Resource belong to another subscription
            resource.SubscriptionId = Guid.NewGuid().ToString();

            DateTime preResetExpirationDate = DateTime.Now.Add(new TimeSpan(730, 0, 0, 0, 0));
            resource.ConfirmedOwner = true;
            resource.Status = ResourceStatus.Expired;
            resource.ExpirationDate = preResetExpirationDate;

            try
            {
                // Expect exception
                ResourceOperationsUtil.ResetResource(resource, subscription);
                Assert.Fail("No exception about wrong subscription thrown.");
            }
            catch (ArgumentException)
            {
                // Test succeeded
            }
        }

        [TestMethod]
        public void TestResetResourceHappyFlow()
        {
            // Let's create resource and subscription to test
            Resource resource = CreateResource();
            resource.ConfirmedOwner = true;
            Subscription subscription = CreateSubscription();
            resource.SubscriptionId = subscription.Id;

            DateTime preResetExpirationDate = DateTime.Now.Add(new TimeSpan(730, 0, 0, 0, 0));
            resource.ConfirmedOwner = true;
            resource.Status = ResourceStatus.Expired;
            resource.ExpirationDate = preResetExpirationDate;

            ResourceOperationsUtil.ResetResource(resource, subscription);

            // Resource properties were changed
            Assert.IsFalse(resource.ConfirmedOwner);
            Assert.AreEqual(ResourceStatus.Valid, resource.Status);
            Assert.AreNotEqual(preResetExpirationDate, resource.ExpirationDate);

            // Expect received expiration date greater than current date
            // Expect received expiration date difference with current data is about to established by subscription properties for unclaimed resources expiration interval
            Assert.IsTrue(resource.ExpirationDate > DateTime.Now);
            Assert.IsTrue(Math.Abs(resource.ExpirationDate.Subtract(DateTime.Now).Days - subscription.ExpirationUnclaimedIntervalInDays) < 2);
        }
    }
}

