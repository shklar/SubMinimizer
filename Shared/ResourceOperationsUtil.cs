using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Text.RegularExpressions;
using CogsMinimizer.Shared;
using Subscription = CogsMinimizer.Shared.Subscription;

namespace CogsMinimizer.Shared
{
    public static class ResourceOperationsUtil
    {
        public static void ResetResources(List<Resource> resourceList , Subscription subscription)
        {
            Diagnostics.EnsureArgumentNotNull(() => resourceList);
            Diagnostics.EnsureArgumentNotNull(() => subscription);

            // update resources properties and store them in resource list.
            // after resources properties update we'll update them in database by separate cycle since updating some resource while data reader opened throws exception
            foreach (Resource resource in resourceList)
            {
                // Add for resetting only resources
                if (resource.SubscriptionId != subscription.Id)
                {
                    continue;
                }

                resource.ConfirmedOwner = false;

                resource.ExpirationDate = GetNewExpirationDate(subscription, resource);
                resource.Status = ResourceStatus.Valid;
            }
        }
        public static bool HasExpired(Resource resource)
        {
            Diagnostics.EnsureArgumentNotNull(() => resource);

            return resource.ExpirationDate.Date < DateTime.UtcNow.Date;
        }

        public static DateTime GetNewReserveDate(Subscription subscription, Resource resource)
        {
            Diagnostics.EnsureArgumentNotNull(() => subscription);
            Diagnostics.EnsureArgumentNotNull(() => resource);

            return DateTime.UtcNow.AddDays(subscription.ReserveIntervalInDays);
        }

        public static DateTime GetNewExpirationDate(Subscription subscription, Resource resource)
        {
            Diagnostics.EnsureArgumentNotNull(() => subscription);
            Diagnostics.EnsureArgumentNotNull(() => resource);

            return DateTime.UtcNow.AddDays(resource.ConfirmedOwner ? subscription.ExpirationIntervalInDays : subscription.ExpirationUnclaimedIntervalInDays);
        }
    }
}