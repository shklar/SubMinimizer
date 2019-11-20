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
        /// <summary>
        ///  Makes resource unclaimed, valid, sets its expiration date to established for subscription value for unclaimed resources  
        /// </summary>
        /// <param name="resource">Resource to proceed</param>
        /// <param name="subscription">Subscription of the resource</param>
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
            resource.Owner = string.Empty;

            resource.ExpirationDate = GetNewExpirationDate(subscription, resource);
            resource.Status = ResourceStatus.Valid;
        }

        /// <summary>
        /// Checks if resource is expired
        /// </summary>
        /// <param name="resource">Resource to proceed</param>
        /// <returns>True if resource is expired false otherwise</returns>6
        public static bool HasExpired(Resource resource)
        {
            Diagnostics.EnsureArgumentNotNull(() => resource);

            return resource.ExpirationDate.Date < DateTime.UtcNow.Date;
        }

        /// <summary>
        /// Returns reserve data for specified resource
        /// </summary>
        /// <param name="resource">Resource to proceed</param>
        /// <param name="subscription">Subscription of the resource</param>
        /// <returns>New reservation data</returns>
        public static DateTime GetNewReserveDate(Subscription subscription, Resource resource)
        {
            Diagnostics.EnsureArgumentNotNull(() => subscription);
            Diagnostics.EnsureArgumentNotNull(() => resource);

            return DateTime.UtcNow.AddDays(subscription.ReserveIntervalInDays);
        }

        /// <summary>
        /// Returns  expiration data for specified resource
        /// </summary>
        /// <param name="resource">Resource to proceed</param>
        /// <param name="subscription">Subscription of the resource</param>
        /// <returns>New  expiration data</returns>
        public static DateTime GetNewExpirationDate(Subscription subscription, Resource resource)
        {
            Diagnostics.EnsureArgumentNotNull(() => subscription);
            Diagnostics.EnsureArgumentNotNull(() => resource);

            return DateTime.UtcNow.AddDays(resource.ConfirmedOwner ? subscription.ExpirationIntervalInDays : subscription.ExpirationUnclaimedIntervalInDays);
        }
    }
}