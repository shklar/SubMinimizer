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
using Microsoft.Rest.Azure;

using ResourceManagementClient = Microsoft.Azure.Management.ResourceManager.ResourceManagementClient;
using Subscription = CogsMinimizer.Shared.Subscription;
using Resource = CogsMinimizer.Shared.Resource;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SubMinimizerTests
{
    [TestClass]
    public class SubMinimizerEndToEndTests
    {

        private Resource GetAnyResource(string subscrId)
        {
            try
            {
                using (DataAccess db = new DataAccess())
                {
                    IEnumerable<Resource> subscrResources = db.Resources.Where(r => r.SubscriptionId == subscrId);
                    return subscrResources.FirstOrDefault();
                }
            }
            catch (Exception)
            {
                Assert.Fail("SubMinimizer database connectivity is absent.");
                return null;
            }
        }


        [TestMethod]
        public void TestGetResourceManagementClient()
        {
            string orgId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
            string subscrId = "bcbd775a-813c-46e8-afe5-1a66912e0f03";
            string providerName = "Microsoft.Portal";

            // may throw, we don't  catch, allow  test fail with exception
            ResourceManagementClient resourceManagementClient = AzureResourceManagerUtil.GetAppResourceManagementClient(subscrId, orgId);

            Assert.IsNotNull(resourceManagementClient);
            IEnumerable<Provider> providers = resourceManagementClient.Providers.List();
            Assert.IsNotNull(providers);
            Provider provider = providers.Where(p => p.NamespaceProperty == providerName).FirstOrDefault();
            Assert.IsNotNull(provider);
            Assert.AreEqual(providerName, provider.NamespaceProperty);
        }

        [TestMethod]
        public void TestGetApiVersion()
        {
            string orgId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
            string subscrId = "bcbd775a-813c-46e8-afe5-1a66912e0f03";

            // get any existing resource
            Resource anyResource = GetAnyResource(subscrId);
            Assert.IsNotNull(anyResource);

            // may throw, we don't  catch, allow  test fail with exception
            ResourceManagementClient resourceManagementClient = AzureResourceManagerUtil.GetAppResourceManagementClient(subscrId, orgId);

            Assert.IsNotNull(resourceManagementClient);

            // Trying to get API version for existing resource
            // may throw, we don't  catch, allow  test fail with exception
            List<string> apiVersionList = new List<string>(); 
            ResourceOperationResult result = AzureResourceManagerUtil.GetApiVersionsSupportedForResource(resourceManagementClient, anyResource, ref apiVersionList);

            // Expect operation succeeded, apiVersion set not null
            Assert.AreEqual(ResourceOperationStatus.Succeeded, result.Result);
            Assert.IsNotNull(apiVersionList);
            Assert.IsTrue(apiVersionList.Count > 0);

        }


        [TestMethod]
        public void TestGetNotExistingResourceType()
        {
            string orgId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
            string subscrId = "bcbd775a-813c-46e8-afe5-1a66912e0f03";

            // Get any resource
            Resource anyResource = GetAnyResource(subscrId);
            Assert.IsNotNull(anyResource);

            ResourceManagementClient resourceManagementClient = AzureResourceManagerUtil.GetAppResourceManagementClient(subscrId, orgId);
            Assert.IsNotNull(resourceManagementClient);

            Resource notExistingResource = anyResource;
            notExistingResource.AzureResourceIdentifier += "~ not existing ~" + Guid.NewGuid().ToString();
            notExistingResource.Type += "~ not existing ~" + Guid.NewGuid().ToString();


            // Trying get resource type for given resource
            // may throw, we don't  catch, allow  test fail with exception
            ProviderResourceType resourceType = null;
            ResourceOperationResult result = AzureResourceManagerUtil.GetResourceType(resourceManagementClient, notExistingResource, ref resourceType);

            // Expect retrieval success, not null retrieved type, it's name same as given resource one
            Assert.AreEqual(ResourceOperationStatus.Failed, result.Result);
            Assert.AreEqual(FailureReason.ResourceTypeNotFound, result.FailureReason);
        }

        [TestMethod]
        public void TestGetResourceType()
        {
            string orgId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
            string subscrId = "bcbd775a-813c-46e8-afe5-1a66912e0f03";

            // Get any resource
            Resource anyResource = GetAnyResource(subscrId);
            Assert.IsNotNull(anyResource);

            ResourceManagementClient resourceManagementClient = AzureResourceManagerUtil.GetAppResourceManagementClient(subscrId, orgId);
            Assert.IsNotNull(resourceManagementClient);
            string[] resourceTypeParts = anyResource.Type.Split(new char[] { '/' });
            Assert.AreEqual(2, resourceTypeParts.Count());


            string anyResourceType = resourceTypeParts[1];

            // Trying get resource type for given resource
            // may throw, we don't  catch, allow  test fail with exception
            ProviderResourceType resourceType = null;
            ResourceOperationResult result = AzureResourceManagerUtil.GetResourceType(resourceManagementClient, anyResource, ref resourceType);

            // Expect retrieval success, not null retrieved type, it's name same as given resource one
            Assert.AreEqual(ResourceOperationStatus.Succeeded, result.Result);
            Assert.IsNotNull(resourceType);
            Assert.AreEqual(anyResourceType, resourceType.ResourceType);
        }

        [TestMethod]
        public void TestDeleteNonExistingResource()
        {
            string orgId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
            string subscrId = "bcbd775a-813c-46e8-afe5-1a66912e0f03";

            try
            {
                // get any existing resource  
                Resource anyExistingResource = GetAnyResource(subscrId);
                Assert.IsNotNull(anyExistingResource);

                ResourceManagementClient resourceManagementClient = AzureResourceManagerUtil.GetAppResourceManagementClient(subscrId, orgId);
                Assert.IsNotNull(resourceManagementClient);

                // let's check resource doesn't exist for sure
                // for this let's retrieve valid api version for working with such resource types
                List<string> apiVersionList = new List<string>();
                ResourceOperationResult apiVersionGetResult = AzureResourceManagerUtil.GetApiVersionsSupportedForResource(resourceManagementClient, anyExistingResource, ref apiVersionList);
                Assert.AreEqual(ResourceOperationStatus.Succeeded, apiVersionGetResult.Result);
                Assert.IsTrue(apiVersionList.Count > 0);
                string apiVersion = apiVersionList[0];

                Resource notExistingResource = anyExistingResource;
                notExistingResource.AzureResourceIdentifier += "~ not existing ~" + Guid.NewGuid().ToString();

                // Trying retrieve resource expect it doesn't exist
                GenericResource azureResource = AzureResourceManagerUtil.GetAzureResource(resourceManagementClient, notExistingResource.AzureResourceIdentifier, apiVersion, new TestTracer());
                Assert.IsNull(azureResource);

                // Trying delete not existing resource 
                ResourceOperationResult result = AzureResourceManagerUtil.DeleteAzureResource(resourceManagementClient, notExistingResource, new TestTracer());

                // Expect succeeded operation, if resource  isn't found assume it was deleted manually, delete method returns success. 
                Assert.AreEqual(ResourceOperationStatus.Succeeded, result.Result);
            }
            catch (Exception)
            {
                Assert.Fail("Current user have no access to test subscription");
            }
        }

        [TestMethod]
        public void TestDeleteNonExistingResourceWithWrongResourceType()
        {
            string orgId = "72f988bf-86f1-41af-91ab-2d7cd011db47";
            string subscrId = "bcbd775a-813c-46e8-afe5-1a66912e0f03";

            try
            {
                // get any existing resource  
                Resource anyExistingResource = GetAnyResource(subscrId);
                Assert.IsNotNull(anyExistingResource);

                ResourceManagementClient resourceManagementClient = AzureResourceManagerUtil.GetAppResourceManagementClient(subscrId, orgId);
                Assert.IsNotNull(resourceManagementClient);

                // let's check resource doesn't exist for sure
                // for this let's retrieve valid api version for working with such resource types
                List<string> apiVersionList = new List<string>();
                ResourceOperationResult apiVersionGetResult = AzureResourceManagerUtil.GetApiVersionsSupportedForResource(resourceManagementClient, anyExistingResource, ref apiVersionList);
                Assert.AreEqual(ResourceOperationStatus.Succeeded, apiVersionGetResult.Result);
                Assert.IsTrue(apiVersionList.Count > 0);
                string apiVersion = apiVersionList[0];

                Resource notExistingResource = anyExistingResource;
                notExistingResource.AzureResourceIdentifier += "~ not existing ~" + Guid.NewGuid().ToString();
                notExistingResource.Type += "~ not existing ~" + Guid.NewGuid().ToString();

                // Trying retrieve resource expect it doesn't exist
                GenericResource azureResource = AzureResourceManagerUtil.GetAzureResource(resourceManagementClient, notExistingResource.AzureResourceIdentifier, apiVersion, new TestTracer());
                Assert.IsNull(azureResource);

                // Trying delete not existing resource with wrong resource type.
                // Should fail at api versions list retrieval
                // Expect delete request failed
                ResourceOperationResult result = AzureResourceManagerUtil.DeleteAzureResource(resourceManagementClient, notExistingResource, new TestTracer());

                // Expect succeeded operation, if resource  isn't found assume it was deleted manually, delete method returns success. 
                Assert.AreEqual(ResourceOperationStatus.Failed, result.Result);
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
