using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CogsMinimizer.Shared
{
    public class DataAccessProvider : IDataAccess
    {
        DataAccess m_dataAccess;
        public DataAccessProvider(DataAccess da)
        {
            m_dataAccess = da;
        }

        public DbSet<Subscription> Subscriptions
        {
            get
            {
                return m_dataAccess.Subscriptions;
            }

            set
            {
                m_dataAccess.Subscriptions = value;
            }
        }

        public DbSet<Resource> Resources
        {
            get
            {
                return m_dataAccess.Resources;
            }

            set
            {
                m_dataAccess.Resources = value;
            }
        }

        public int SaveChanges()
        {
            return m_dataAccess.SaveChanges();
        }
    }
}
