using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Proxy
{
    public class PooledConnection
    {
        public IDataSource DataSource { get; set; }
        public DateTime LastUsed { get; set; }
    }

    public class DataSourceMetrics
    {
        private long _totalRequests;
        private long _successfulRequests;
        private long _failedRequests;

        public long TotalRequests
        {
            get => Interlocked.Read(ref _totalRequests); // Thread-safe read
            set => Interlocked.Exchange(ref _totalRequests, value); // Thread-safe write
        }

        public long SuccessfulRequests
        {
            get => Interlocked.Read(ref _successfulRequests);
            set => Interlocked.Exchange(ref _successfulRequests, value);
        }

        public long FailedRequests
        {
            get => Interlocked.Read(ref _failedRequests);
            set => Interlocked.Exchange(ref _failedRequests, value);
        }

        public double AverageResponseTime { get; set; }
        public DateTime LastRequested { get; set; }
        public DateTime LastSuccessful { get; set; }
        public long CircuitBreaks { get; set; }
        public DateTime LastChecked { get; set; }

        // Public methods for thread-safe increments
        public void IncrementTotalRequests()
        {
            Interlocked.Increment(ref _totalRequests);
        }

        public void IncrementFailedRequests()
        {
            Interlocked.Increment(ref _failedRequests);
        }

        public void IncrementSuccessfulRequests()
        {
            Interlocked.Increment(ref _successfulRequests);
        }
    }

    public class FailoverEventArgs : EventArgs
    {
        public string FromDataSource { get; set; }
        public string ToDataSource { get; set; }
    }
    public class CacheEntry
    {
        public object Data { get; set; }
        public DateTime Expiration { get; set; }
    }
    public class ProxyDataSourceOptions
    {
        public int MaxRetries { get; set; } = 3;
        public int RetryDelayMilliseconds { get; set; } = 1000;
        public int HealthCheckIntervalMilliseconds { get; set; } = 30000;
        public int FailureThreshold { get; set; } = 5;
        public TimeSpan CircuitResetTimeout { get; set; } = TimeSpan.FromMinutes(5);
        public bool EnableCaching { get; set; } = true;
        public TimeSpan DefaultCacheExpiration { get; set; } = TimeSpan.FromMinutes(5);
        public bool EnableLoadBalancing { get; set; } = true;
    }

}
