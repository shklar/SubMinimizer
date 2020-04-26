using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Linq.Expressions;
using CogsMinimizer.Shared;
using Microsoft.Azure.Management.ResourceManager.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Resource = CogsMinimizer.Shared.Resource;
using Subscription = CogsMinimizer.Shared.Subscription;

namespace SubMinimizerTests
{
    [TestClass]
    public class SubscriptionAnalyzerTests
    {
        //
        //Consts
        //
        public readonly string SUBSCRIPTION_ID = "A69C3703-5D68-4735-AF29-71396481D861";
        public readonly string RESOURCE_GROUP_NAME = "Resource_Group_Name";
        public readonly string OWNER_EMAIL = "owner@company.com";
        public readonly int DELETE_AFTER_DAYS = 3;


        #region Tests
        [TestMethod]
        public void TestExpiredResourceRemainsExpired()
        {
            var testTime = DateTime.UtcNow;
            //
            //Arrange
            //

            //Expired 2 days ago
            var resource1 = CreateResource();
            resource1.Expired = true;
            resource1.Status = ResourceStatus.Expired;
            resource1.ExpirationDate = testTime.Date.Subtract(new TimeSpan(2, 0, 0, 0));

            var resourceList = new List<Resource> { resource1};

            SubscriptionAnalyzer subscriptionsAnalyzer = TestPreparation(resourceList);

            //Act
            var analysisResult = subscriptionsAnalyzer.AnalyzeSubscription();

            //Assert
            Assert.IsTrue(analysisResult.ExpiredResources.Count == 1);
            Assert.IsTrue(analysisResult.ValidResources.Count == 0);
            Assert.IsTrue(analysisResult.MarkedForDeleteResources.Count == 0);
            Assert.IsTrue(analysisResult.NewResources.Count == 0);
            Assert.IsTrue(analysisResult.NotFoundResources.Count == 0);

        }

        [TestMethod]
        public void TestValidFutureExpirationResourceRemainsValid()
        {
            var testTime = DateTime.UtcNow;
            
            //
            //Arrange
            //

            //Expires in 2 days
            var resource2 = CreateResource();
            resource2.Expired = false;
            resource2.Status = ResourceStatus.Valid;
            resource2.ExpirationDate = testTime.Date.Add(new TimeSpan(2, 0, 0, 0));

            var resourceList = new List<Resource> { resource2};

            SubscriptionAnalyzer subscriptionsAnalyzer = TestPreparation(resourceList);

            //Act
            var analysisResult = subscriptionsAnalyzer.AnalyzeSubscription();

            //Assert
            Assert.IsTrue(analysisResult.ExpiredResources.Count == 0);
            Assert.IsTrue(analysisResult.ValidResources.Count == 1);
            Assert.IsTrue(analysisResult.MarkedForDeleteResources.Count == 0);
            Assert.IsTrue(analysisResult.NewResources.Count == 0);
            Assert.IsTrue(analysisResult.NotFoundResources.Count == 0);
        }

        [TestMethod]
        public void TestValidPastExpirationResourceBecomesExpired()
        {
            var testTime = DateTime.UtcNow;

            //
            //Arrange
            //

            //Should have expired by now
            var resource3 = CreateResource();
            resource3.Expired = false;
            resource3.Status = ResourceStatus.Valid;
            resource3.ExpirationDate = testTime.Date.Subtract(new TimeSpan(1, 0, 0, 0));

            var resourceList = new List<Resource> { resource3 };

            SubscriptionAnalyzer subscriptionsAnalyzer = TestPreparation(resourceList);

            //Act
            var analysisResult = subscriptionsAnalyzer.AnalyzeSubscription();

            //Assert
            Assert.IsTrue(analysisResult.ExpiredResources.Count == 1);
            Assert.IsTrue(analysisResult.ValidResources.Count == 0);
            Assert.IsTrue(analysisResult.MarkedForDeleteResources.Count == 0);
            Assert.IsTrue(analysisResult.NewResources.Count == 0);
            Assert.IsTrue(analysisResult.NotFoundResources.Count == 0);
        }

        [TestMethod]
        public void TestNewResourceFound()
        {
            var testTime = DateTime.UtcNow;

            //Arrange

            //New resource
            var resource4 = CreateResource();
            resource4.Expired = false;
 
            var resourceList = new List<Resource> { resource4 };

            SubscriptionAnalyzer subscriptionsAnalyzer = TestPreparation(resourceList, new List<Resource>());

            //Act
            var analysisResult = subscriptionsAnalyzer.AnalyzeSubscription();

            //Assert
            Assert.IsTrue(analysisResult.ExpiredResources.Count == 0);
            Assert.IsTrue(analysisResult.ValidResources.Count == 0);
            Assert.IsTrue(analysisResult.MarkedForDeleteResources.Count == 0);
            Assert.IsTrue(analysisResult.NewResources.Count == 1);
            Assert.IsTrue(analysisResult.NotFoundResources.Count == 0);
        }

        [TestMethod]
        public void TestValidResourceWentMissing()
        {
            var testTime = DateTime.UtcNow;

            //Arrange
            var resource5 = CreateResource();
            resource5.Expired = false;
            resource5.Status = ResourceStatus.Valid;

            var resourceList = new List<Resource> { resource5 };

            SubscriptionAnalyzer subscriptionsAnalyzer = TestPreparation(new List<Resource>(), resourceList);

            //Act
            var analysisResult = subscriptionsAnalyzer.AnalyzeSubscription();

            //Assert
            Assert.IsTrue(analysisResult.ExpiredResources.Count == 0);
            Assert.IsTrue(analysisResult.ValidResources.Count == 0);
            Assert.IsTrue(analysisResult.MarkedForDeleteResources.Count == 0);
            Assert.IsTrue(analysisResult.NewResources.Count == 0);
            Assert.IsTrue(analysisResult.NotFoundResources.Count == 1);
        }


        [TestMethod]
        public void TestExpiredResourceBecomesMarkedForDeleteOnTime()
        {
            var testTime = DateTime.UtcNow;

            //Arrange

            //Expired, not ready for delete
            var resource1 = CreateResource();
            resource1.Expired = true;
            resource1.Status = ResourceStatus.Expired;
            resource1.ExpirationDate = testTime.Date.Subtract(new TimeSpan(DELETE_AFTER_DAYS - 1, 0, 0, 0));

            //Expired, ready for delete
            var resource2 = CreateResource();
            resource2.Expired = true;
            resource2.Status = ResourceStatus.Expired;
            resource2.ExpirationDate = testTime.Date.Subtract(new TimeSpan(DELETE_AFTER_DAYS , 0, 0, 0));

            var resourceList = new List<Resource> { resource1, resource2 };

            SubscriptionAnalyzer subscriptionsAnalyzer = TestPreparation(resourceList);

            //Act
            var analysisResult = subscriptionsAnalyzer.AnalyzeSubscription();

            //Assert
            Assert.IsTrue(analysisResult.ExpiredResources.Count == 1);
            Assert.IsTrue(analysisResult.ValidResources.Count == 0);
            Assert.IsTrue(analysisResult.MarkedForDeleteResources.Count == 1);
            Assert.IsTrue(analysisResult.NewResources.Count == 0);
            Assert.IsTrue(analysisResult.NotFoundResources.Count == 0);
        }

        [TestMethod]
        public void TestMarkedForDeleteResourceRemainsMarkedForDelete()
        {
            var testTime = DateTime.UtcNow;

            //Arrange

            //Expired, marked for delete
            var resource1 = CreateResource();
            resource1.Expired = true;
            resource1.Status = ResourceStatus.MarkedForDeletion;
            resource1.ExpirationDate = testTime.Date.Subtract(new TimeSpan(DELETE_AFTER_DAYS + 1, 0, 0, 0));

          
            var resourceList = new List<Resource> { resource1 };

            SubscriptionAnalyzer subscriptionsAnalyzer = TestPreparation(resourceList);

            //Act
            var analysisResult = subscriptionsAnalyzer.AnalyzeSubscription();

            //Assert
            Assert.IsTrue(analysisResult.ExpiredResources.Count == 0);
            Assert.IsTrue(analysisResult.ValidResources.Count == 0);
            Assert.IsTrue(analysisResult.MarkedForDeleteResources.Count == 1);
            Assert.IsTrue(analysisResult.NewResources.Count == 0);
            Assert.IsTrue(analysisResult.NotFoundResources.Count == 0);
        }

        #endregion

        #region Test Preparation

        //The default implementation uses the same list for both
        private SubscriptionAnalyzer TestPreparation(List<Resource> resourceList)
        {
            return TestPreparation(resourceList, resourceList);
        }

        private SubscriptionAnalyzer TestPreparation(List<Resource> AzureResources, List<Resource> DBResourcesources)
        {
            var subscription = CreateSubscription();

            //Resources

            var resourceData = DBResourcesources.AsQueryable();
            var mockResourceSet = new Mock<MockableDbSetWithExtensions<Resource>>();
            mockResourceSet.As<IQueryable<Resource>>().Setup(m => m.Provider).Returns(resourceData.Provider);
            mockResourceSet.As<IQueryable<Resource>>().Setup(m => m.Expression).Returns(resourceData.Expression);
            mockResourceSet.As<IQueryable<Resource>>().Setup(m => m.ElementType).Returns(resourceData.ElementType);
            mockResourceSet.As<IQueryable<Resource>>().Setup(m => m.GetEnumerator()).Returns(resourceData.GetEnumerator());
            mockResourceSet.As<IEnumerable<Resource>>().Setup(m => m.GetEnumerator()).Returns(resourceData.GetEnumerator());
            //mockResourceSet.Setup(m=>m.AddOrUpdate<Resource>()).Callback<Resource>(s =>  { return;});

            //Subscriptions
            var subscriptionData = new List<Subscription> { subscription }.AsQueryable();
            var mockSubscriptionSet = new Mock<MockableDbSetWithExtensions<Subscription>>();
            mockSubscriptionSet.As<IQueryable<Subscription>>().Setup(m => m.Provider).Returns(subscriptionData.Provider);
            mockSubscriptionSet.As<IQueryable<Subscription>>().Setup(m => m.Expression).Returns(subscriptionData.Expression);
            mockSubscriptionSet.As<IQueryable<Subscription>>().Setup(m => m.ElementType).Returns(subscriptionData.ElementType);
            mockSubscriptionSet.As<IQueryable<Subscription>>().Setup(m => m.GetEnumerator()).Returns(subscriptionData.GetEnumerator());
            mockSubscriptionSet.As<IEnumerable<Subscription>>().Setup(m => m.GetEnumerator()).Returns(subscriptionData.GetEnumerator());

            //DataAccess
            var mockDataAccess = new Mock<IDataAccess>();
            mockDataAccess.Setup(m => m.Subscriptions).Returns(mockSubscriptionSet.Object);
            mockDataAccess.Setup(m => m.Resources).Returns(mockResourceSet.Object);

            //Azure resource management
            var mockAzureResourceManagement = new Mock<IAzureResourceManagement>();
            mockAzureResourceManagement.Setup(x => x.ServicePrincipalHasReadAccessToSubscription(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);
            mockAzureResourceManagement.Setup(x => x.GetSubscriptionAdmins(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new List<string> { OWNER_EMAIL });

            var AzureResourceList = AzureResources.Select(x => new GenericResource(x.Id));
            mockAzureResourceManagement.Setup(x => x.GetResourceList(It.IsAny<string>())).Returns(AzureResourceList);

            var rg = new ResourceGroup();
            rg.Name = RESOURCE_GROUP_NAME;
            var resourceGroups = new List<ResourceGroup> { rg };
            mockAzureResourceManagement.Setup(x => x.GetResourceGroups()).Returns(resourceGroups);

            var subscriptionsAnalyzer = new SubscriptionAnalyzer(mockDataAccess.Object, subscription, mockAzureResourceManagement.Object, TracerFactory.CreateTracer());
            return subscriptionsAnalyzer;
        }
       

        private Resource CreateResource()
        {
            var ID = Guid.NewGuid().ToString();
            var resource = new Resource
            {
                Id = ID,
                AzureResourceIdentifier = ID,
                Name = "ResourceName",
                Type = "ResourceType",
                ResourceGroup = RESOURCE_GROUP_NAME,
                FirstFoundDate = new DateTime(2010, 1, 1),
                ExpirationDate = new DateTime(2020, 9, 9),
                LastVisitedDate = new DateTime(2020, 1, 1),
                Owner = OWNER_EMAIL,
                ConfirmedOwner = true,
                Expired = true,
                SubscriptionId = SUBSCRIPTION_ID,
                Status = ResourceStatus.Expired
            };

            return resource;
        }

        private Subscription CreateSubscription()
        {
            var subscription = new Subscription();
            subscription.Id = SUBSCRIPTION_ID;
            subscription.DisplayName = "SubscriptionName";
            subscription.OrganizationId = "OrgId";
            subscription.DeleteIntervalInDays = DELETE_AFTER_DAYS;

            return subscription;
        }
    }

    public abstract class MockableDbSetWithExtensions<T> : DbSet<T>
    where T : class
    {
        public abstract void AddOrUpdate(params T[] entities);
        public abstract void AddOrUpdate(Expression<Func<T, object>>
             identifierExpression, params T[] entities);
    }

    #endregion
}

