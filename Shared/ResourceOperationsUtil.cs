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
        public static void ResetResource(Resource resource, Subscription subscription)
        {
            Diagnostics.EnsureArgumentNotNull(() => resource);
            Diagnostics.EnsureArgumentNotNull(() => subscription);

            // Add for resetting only resources
            if (resource.SubscriptionId != subscription.Id)
            {
                throw new ArgumentException(string.Format("Given resource with ID '{0}' doesn't belong to specified subscription with ID '{1}'", resource.Id, subscription.Id));
            }

            resource.ConfirmedOwner = false;

            resource.ExpirationDate = GetNewExpirationDate(subscription, resource);
            resource.Status = ResourceStatus.Valid;
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