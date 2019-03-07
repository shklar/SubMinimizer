using System.Collections.Generic;

namespace CogsMinimizer.Shared
{
    /// <summary>
    /// An in memory token cache to hold the ADAL Tokens
    /// </summary>
    public class InMemoryTokenCache
    {
        private static InMemoryTokenCache s_instance;
        private static object s_lock = new object();

        public static InMemoryTokenCache Instance
        {
            get
            {
                if (s_instance == null)
                {
                    lock (s_lock)
                    {
                        if (s_instance == null)
                        {
                            s_instance = new InMemoryTokenCache();
                        }
                    }
                }
                return s_instance;
            }
            private set
            {
                s_instance = value;
            }
        }

        private InMemoryTokenCache()
        {
            PerUserTokenCacheList = new HashSet<PerUserTokenCache>();
        }
        public HashSet<PerUserTokenCache> PerUserTokenCacheList { get; set; }    
    }
}
