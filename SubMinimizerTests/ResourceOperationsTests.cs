using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CogsMinimizer.Shared;

namespace SubMinimizerTests
{
    [TestClass]
    public class ResourceOperationsTests
    {
        #region HasExpired
        [TestMethod]
        public void TestHasExpired_DayOldResource_ConsideredExpired()
        {
            // Let's create resource 
            Resource resource = TestUtils.CreateResource();

            // Make resource expired
            resource.ExpirationDate = DateTime.UtcNow.Subtract(new TimeSpan(1, 0, 0, 0));

            // Expect resource be expired
            Assert.IsTrue(ResourceOperationsUtil.HasExpired(resource));
        }

        [TestMethod]
        public void TestHasExpired_CurrentDayResource_NotConsideredExpired()
        {
            // Let's create resource and subscription to test
            Resource resource = TestUtils.CreateResource();
            resource.ExpirationDate = DateTime.UtcNow;

            // Expect resource not be expired
            Assert.IsFalse(ResourceOperationsUtil.HasExpired(resource));
        }

        [TestMethod]
        public void TestHasExpired_FutureExpirationResource_NotConsideredExpired()
        {
            // Let's create resource and subscription to test
            Resource resource = TestUtils.CreateResource();
            resource.ExpirationDate = DateTime.UtcNow.Add(new TimeSpan(1, 0, 0, 0));

            // Expect resource not be expired
            Assert.IsFalse(ResourceOperationsUtil.HasExpired(resource));
        }
        #endregion HasExpired

        [TestMethod]
        public void TestGetExpirationDate()
        {
            // Let's create resource and subscription to test
            Resource resource = TestUtils.CreateResource();
            resource.ConfirmedOwner = true;
            Subscription subscription = TestUtils.CreateSubscription();
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
            Resource resource = TestUtils.CreateResource();
            Subscription subscription = TestUtils.CreateSubscription();
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
            Resource resource = TestUtils.CreateResource();
            resource.ConfirmedOwner = true;
            Subscription subscription = TestUtils.CreateSubscription();
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
            Resource resource = TestUtils.CreateResource();
            resource.ConfirmedOwner = true;
            Subscription subscription = TestUtils.CreateSubscription();


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
            Resource resource = TestUtils.CreateResource();
            resource.ConfirmedOwner = true;
            Subscription subscription = TestUtils.CreateSubscription();
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
