using System;
using System.IO;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.IdentityModel.Claims;
using System.Linq;
using System.Reflection;
using CogsMinimizer.Shared;
using Microsoft.Azure.Management.Authorization;
using Microsoft.Azure.Management.Authorization.Models;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.ResourceManager.Models;
using ResourceManagementClient = Microsoft.Azure.Management.ResourceManager.ResourceManagementClient;
using Subscription = CogsMinimizer.Shared.Subscription;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SubMinimizerTests
{
    [TestClass]
    public class SubMinimizerTests
    {
        private GenericResource GetAnyResource(string orgId, string subscrId)
        {
            ResourceManagementClient resourceManagementClient = AzureResourceManagerUtil.GetAppResourceManagementClient(subscrId, orgId);
            Assert.IsNotNull(resourceManagementClient);
            var resources = resourceManagementClient.Resources.List();
            Assert.IsNotNull(resources);
            Assert.IsTrue(resources.Count() > 0);
            return resources.First();
        }


        [TestMethod]
        public void TestGetProviderList()
        {
            string orgId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
            string subscrId = "bcbd775a-813c-46e8-afe5-1a66912e0f03";
            string providerName = "Microsoft.Portal";

            try
            {
                ResourceManagementClient resourceManagementClient = AzureResourceManagerUtil.GetAppResourceManagementClient(subscrId, orgId);
                Assert.IsNotNull(resourceManagementClient);
                IEnumerable<Provider> providers = AzureResourceManagerUtil.GetResourceProviderList(resourceManagementClient);
                Assert.IsNotNull(providers);
                Provider provider = providers.Where(p => p.NamespaceProperty == providerName).FirstOrDefault();
                Assert.IsNotNull(provider);
                Assert.AreEqual(providerName, provider.NamespaceProperty);
            }
            catch (Exception)
            {
                Assert.Fail("Current user have no access to test subscription");
            }
        }

        [TestMethod]
        public void TestGetApiVersion()
        {
            string orgId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
            string subscrId = "bcbd775a-813c-46e8-afe5-1a66912e0f03";

            try
            {
                // get any existing resource
                GenericResource anyResource = GetAnyResource(orgId, subscrId);
                Assert.IsNotNull(anyResource);

                ResourceManagementClient resourceManagementClient = AzureResourceManagerUtil.GetAppResourceManagementClient(subscrId, orgId);
                Assert.IsNotNull(resourceManagementClient);

                // Trying to get API version for existing resource
                string apiVersion = null;
                ResourceOperationResult result = AzureResourceManagerUtil.GetApiVersionSupportedForResource(resourceManagementClient, anyResource.Id, ref apiVersion);

                // Expect operation succeeded, apiVersion set not null
                Assert.AreEqual(ResourceOperationStatus.Succeeded, result.Result);
                Assert.IsNotNull(apiVersion);
            }
            catch (Exception)
            {
                Assert.Fail("Current user have no access to test subscription");
            }
        }


        [TestMethod]
        public void TestGetApiVersionForNonExistingResource()
        {
            string orgId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
            string subscrId = "bcbd775a-813c-46e8-afe5-1a66912e0f03";

            try
            {
                // get any existing resource
                GenericResource anyResource = GetAnyResource(orgId, subscrId);
                Assert.IsNotNull(anyResource);

                // Corrupt existing resource ID, now it points to not existing resource
                string resourceId = anyResource.Id + "- not existing -" + Guid.NewGuid().ToString();

                ResourceManagementClient resourceManagementClient = AzureResourceManagerUtil.GetAppResourceManagementClient(subscrId, orgId);
                Assert.IsNotNull(resourceManagementClient);

                // Trying to get resource type for not existing resource
                string apiVersion = null;
                ResourceOperationResult result = AzureResourceManagerUtil.GetApiVersionSupportedForResource(resourceManagementClient, resourceId, ref apiVersion);

                // Expect operation failed with appropriate reason returned, apiVersion null
                Assert.AreEqual(ResourceOperationStatus.Failed, result.Result);
                Assert.AreEqual(FailureReason.ResourceNotFound, result.FailureReason);
                Assert.IsNull(apiVersion);
            }
            catch (Exception)
            {
                Assert.Fail("Current user have no access to test subscription");
            }
        }

        [TestMethod]
        public void TestGetResource()
        {
            string orgId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
            string subscrId = "bcbd775a-813c-46e8-afe5-1a66912e0f03";

            try
            {
                // get any existing resource
                GenericResource anyResource = GetAnyResource(orgId, subscrId);
                Assert.IsNotNull(anyResource);

                ResourceManagementClient resourceManagementClient = AzureResourceManagerUtil.GetAppResourceManagementClient(subscrId, orgId);
                Assert.IsNotNull(resourceManagementClient);
                string apiVersion = null;
                ResourceOperationResult result = AzureResourceManagerUtil.GetApiVersionSupportedForResource(resourceManagementClient, anyResource.Id, ref apiVersion);
                Assert.AreEqual(ResourceOperationStatus.Succeeded, result.Result);
                Assert.IsNotNull(apiVersion);

                // Trying get resource by ID
                GenericResource retrievedResource = AzureResourceManagerUtil.GetAzureResource(resourceManagementClient, anyResource.Id, apiVersion, new TestTracer());

                // Expect retrieved resource not null, its ID same as given
                Assert.IsNotNull(retrievedResource);
                Assert.AreEqual(anyResource.Id, retrievedResource.Id);
            }
            catch (Exception)
            {
                Assert.Fail("Current user have no access to test subscription");
            }
        }


        [TestMethod]
        public void TestGetNonExistingResource()
        {
            string orgId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
            string subscrId = "bcbd775a-813c-46e8-afe5-1a66912e0f03";

            try
            {
                // get any existing resource
                GenericResource anyResource = GetAnyResource(orgId, subscrId);
                Assert.IsNotNull(anyResource);

                ResourceManagementClient resourceManagementClient = AzureResourceManagerUtil.GetAppResourceManagementClient(subscrId, orgId);
                Assert.IsNotNull(resourceManagementClient);
                string apiVersion = null;
                ResourceOperationResult result = AzureResourceManagerUtil.GetApiVersionSupportedForResource(resourceManagementClient, anyResource.Id, ref apiVersion);
                Assert.IsNotNull(apiVersion);

                // Corrupt existing resource ID, now it points to not existing resource
                string resourceId = anyResource.Id + "- not existing -" + Guid.NewGuid().ToString();

                // Trying to get not existing resource
                GenericResource retrievedResource = AzureResourceManagerUtil.GetAzureResource(resourceManagementClient, resourceId, apiVersion, new TestTracer());
                // Expect retrieved resource null
                Assert.IsNull(retrievedResource);
            }
            catch (Exception)
            {
                Assert.Fail("Current user have no access to test subscription");
            }
        }

        [TestMethod]
        public void TestGetResourceType()
        {
            string orgId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
            string subscrId = "bcbd775a-813c-46e8-afe5-1a66912e0f03";

            try
            {
                // Get any resource
                GenericResource anyResource = GetAnyResource(orgId, subscrId);
                Assert.IsNotNull(anyResource);

                ResourceManagementClient resourceManagementClient = AzureResourceManagerUtil.GetAppResourceManagementClient(subscrId, orgId);
                Assert.IsNotNull(resourceManagementClient);
                string anyResourceType = anyResource.Type;
                if (anyResourceType.Contains("/"))
                {
                    anyResourceType = anyResourceType.Substring(anyResourceType.IndexOf("/") + 1);
                }

                // Trying get resource type for given resource
                ProviderResourceType resourceType = AzureResourceManagerUtil.GetResourceType(resourceManagementClient, anyResource.Id);

                // Expect not null retrieved type, it's name same as given resource one
                Assert.IsNotNull(resourceType);
                Assert.AreEqual(anyResourceType, resourceType.ResourceType);
            }
            catch (Exception)
            {
                Assert.Fail("Current user have no access to test subscription");
            }
        }

        [TestMethod]
        public void TestGetNonExistingResourceType()
        {
            string orgId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
            string subscrId = "bcbd775a-813c-46e8-afe5-1a66912e0f03";

            try
            {
                // get any existing resource
                GenericResource anyResource = GetAnyResource(orgId, subscrId);
                Assert.IsNotNull(anyResource);

                ResourceManagementClient resourceManagementClient = AzureResourceManagerUtil.GetAppResourceManagementClient(subscrId, orgId);
                Assert.IsNotNull(resourceManagementClient);
                string anyResourceType = anyResource.Type;
                if (anyResourceType.Contains("/"))
                {
                    anyResourceType = anyResourceType.Substring(anyResourceType.IndexOf("/") + 1);
                }

                // Corrupt existing resource ID, now it points to not existing resource
                string resourceId = anyResource.Id + "- not existing -" + Guid.NewGuid().ToString();

                // Trying get resource type for given resource
                ProviderResourceType resourceType = AzureResourceManagerUtil.GetResourceType(resourceManagementClient, resourceId);

                // Expect not null retrieved type since we pay no attention at resource existence using only resource provider and resource type.
                Assert.IsNotNull(resourceType);
            }
            catch (Exception)
            {
                Assert.Fail("Current user have no access to test subscription");
            }
        }

        [TestMethod]
        public void TestDeleteNonExistingResource()
        {
            string orgId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
            string subscrId = "bcbd775a-813c-46e8-afe5-1a66912e0f03";

            try
            {
                // get any existing resource
                GenericResource anyResource = GetAnyResource(orgId, subscrId);
                Assert.IsNotNull(anyResource);

                // Corrupt existing resource ID, now it points to not existing resource
                string resourceId = anyResource.Id + "- not existing -" + Guid.NewGuid().ToString();

                ResourceManagementClient resourceManagementClient = AzureResourceManagerUtil.GetAppResourceManagementClient(subscrId, orgId);
                Assert.IsNotNull(resourceManagementClient);

                // Trying get resource type for given resource ID
                ResourceOperationResult result = AzureResourceManagerUtil.DeleteAzureResource(resourceManagementClient, resourceId, new TestTracer());

                // Expect succeeded operation, if resource  isn't found assume it was deleted manually, delete method returns success.
                Assert.AreEqual(ResourceOperationStatus.Succeeded, result.Result);
            }
            catch (Exception)
            {
                Assert.Fail("Current user have no access to test subscription");
            }
        }


        [TestMethod]
        public void TestGetFailureReason()
        {
            string responseContent1 = "{\"error\":{\"code\":\"NoRegisteredProviderFound\",\"message\":\"No registered resource provider found for location 'southcentralus' and API version '2016-10-01-i' for type 'networkSecurityGroups'. The supported api-versions are '2014-12-01-preview, 2015-05-01-preview, 2015-06-15, 2016-03-30, 2016-06-01, 2016-07-01, 2016-08-01, 2016-09-01, 2016-10-01'. The supported locations are 'westus, eastus, northeurope, westeurope, eastasia, southeastasia, northcentralus, southcentralus, centralus, eastus2, japaneast, japanwest, brazilsouth, australiaeast, australiasoutheast, centralindia, southindia, canadacentral, canadaeast, westcentralus, westus2, ukwest, uksouth'.\"}}";
            FailureReason reason1 = AzureResourceManagerUtil.GetFailureReason(responseContent1);
            Assert.AreEqual(FailureReason.WrongApiVersion, reason1);

            string responseContent2 = "{\r\n  \"error\": {\r\n    \"code\": \"InUseNetworkSecurityGroupCannotBeDeleted\",\r\n    \"message\": \"Network security group /subscriptions/bcbd775a-813c-46e8-afe5-1a66912e0f03/resourceGroups/evitenres/providers/Microsoft.Network/networkSecurityGroups/eviten-vm-1-nsg cannot be deleted because it is in use by the following resources: /subscriptions/bcbd775a-813c-46e8-afe5-1a66912e0f03/resourceGroups/evitenres/providers/Microsoft.Network/networkInterfaces/eviten-vm-1731.\",\r\n    \"details\": []\r\n  }\r\n}";
            FailureReason reason2 = AzureResourceManagerUtil.GetFailureReason(responseContent2);
            Assert.AreEqual(FailureReason.ResourceInUse, reason2);

            string responseContent3 = "The Resource 'Microsoft.Compute/virtualMachines/eviten-vm-1' under resource group 'evitenres' was not found.";
            FailureReason reason3 = AzureResourceManagerUtil.GetFailureReason(responseContent3);
            Assert.AreEqual(FailureReason.ResourceNotFound, reason3);

            string responseContent4 = "{\"error\":{\"code\":\"ResourceNotFound\",\"message\":\"The Resource 'Microsoft.Portal/dashboards/2a0dd2da-fb4e-4732-aa15-f3bc617f9190- not existing -e7477d6c-68de-4285-909c-d9bb39a7f48b' under resource group 'dashboards' was not found.\"}}";
            FailureReason reason4 = AzureResourceManagerUtil.GetFailureReason(responseContent4);
            Assert.AreEqual(FailureReason.ResourceNotFound, reason4);

        }

    }
}
