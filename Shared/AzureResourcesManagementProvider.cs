using System.Collections.Generic;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.ResourceManager.Models;

namespace CogsMinimizer.Shared
{
    public class AzureResourcesManagementProvider : IAzureResourceManagement
    {
        private ResourceManagementClient m_resourceClient;

        public AzureResourcesManagementProvider(ResourceManagementClient resourceClient)
        {
            m_resourceClient = resourceClient;
        }

        public IEnumerable<ResourceGroup> GetResourceGroups()
        {
            return AzureResourceManagerUtil.GetResourceGroups(m_resourceClient);
        }

        public IEnumerable<GenericResource> GetResourceList(string groupName)
        {
            return AzureResourceManagerUtil.GetResourceList(m_resourceClient, groupName);
        }

        public List<string> GetSubscriptionAdmins(string subscriptionId, string organizationId)
        {
            return AzureResourceManagerUtil.GetSubscriptionAdmins2(subscriptionId, organizationId);
        }

        public bool ServicePrincipalHasReadAccessToSubscription(string subscriptionId, string organizationId)
        {
            return AzureResourceManagerUtil.ServicePrincipalHasReadAccessToSubscription(subscriptionId, organizationId);
        }
    }
}
