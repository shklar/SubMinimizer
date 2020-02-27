using System;
using System.Collections.Generic;
using System.Linq;
using CogsMinimizer.Shared;

namespace SubMinimizerTests
{
    public static class TestUtils
    {
        public static Subscription CreateSubscription()
        {
            Subscription subscription = new Subscription();
            subscription.Id = Guid.NewGuid().ToString();
            subscription.DisplayName = "subscription - " + subscription.Id;
            subscription.ExpirationIntervalInDays = 20;
            subscription.ExpirationUnclaimedIntervalInDays = 10;
            subscription.ReserveIntervalInDays = 100;
            return subscription;
        }

        public static Resource CreateResource()
        {
            Resource resource = new Resource();
            resource.Id = Guid.NewGuid().ToString();
            resource.Name = "resource - " + resource.Id;
            resource.FirstFoundDate = DateTime.UtcNow;
            resource.ExpirationDate = DateTime.UtcNow;
            return resource;
        }
    }
}
