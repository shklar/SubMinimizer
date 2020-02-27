using System.Data.Entity;

namespace CogsMinimizer.Shared
{
    public interface IDataAccess
    {
        DbSet<Subscription> Subscriptions { get; set; }
        DbSet<Resource> Resources { get; set; }
        int SaveChanges();
    }
}
