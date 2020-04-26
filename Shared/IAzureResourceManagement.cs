using System.Collections.Generic;
using Microsoft.Azure.Management.ResourceManager.Models;


namespace CogsMinimizer.Shared
{
    /// <summary>
    /// An interface for providing ARM operations around subscriptions and resources
    /// </summary>
    public interface IAzureResourceManagement
    {
        List<string> GetSubscriptionAdmins(string subscriptionId, string organizationId);
        bool ServicePrincipalHasReadAccessToSubscription(string subscriptionId, string organizationId);
        IEnumerable<GenericResource> GetResourceList(string groupName);
        IEnumerable<ResourceGroup> GetResourceGroups();
    }
}
