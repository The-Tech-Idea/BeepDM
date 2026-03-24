using System;

namespace TheTechIdea.Beep.Proxy
{
    /// <summary>
    /// Thread-safe circuit breaker that supports three states (Closed, Open, HalfOpen),
    /// severity-weighted failure accumulation, and a configurable consecutive-success
    /// threshold before closing from HalfOpen.
    /// </summary>
    public class CircuitBreaker
    {
        private readonly object   _stateLock = new object();
        private CircuitBreakerState _state    = CircuitBreakerState.Closed;
        private DateTime          _lastStateChange     = DateTime.UtcNow;
        private int               _failureCount;
        private int               _consecutiveSuccesses;
        private readonly int      _failureThreshold;
        private readonly int      _successThreshold;
        private readonly TimeSpan _resetTimeout;

        // ── Public read-only state ─────────────────────────────────────

        public CircuitBreakerState State
        {
            get { lock (_stateLock) return _state; }
        }

        public int FailureCount
        {
            get { lock (_stateLock) return _failureCount; }
        }

        public DateTime LastStateChange
        {
            get { lock (_stateLock) return _lastStateChange; }
        }

        // ── Constructor ───────────────────────────────────────────────

        public CircuitBreaker(int failureThreshold, TimeSpan resetTimeout, int successThreshold = 2)
        {
            if (failureThreshold <= 0) throw new ArgumentOutOfRangeException(nameof(failureThreshold));
            if (successThreshold <= 0) throw new ArgumentOutOfRangeException(nameof(successThreshold));

            _failureThreshold = failureThreshold;
            _successThreshold = successThreshold;
            _resetTimeout     = resetTimeout;
        }

        // ── State transitions ─────────────────────────────────────────

        /// <summary>Returns true when a request may proceed through the circuit.</summary>
        public bool CanExecute()
        {
            lock (_stateLock)
            {
                if (_state == CircuitBreakerState.Closed)
                    return true;

                if (_state == CircuitBreakerState.HalfOpen)
                    return true;

                // Open: check if reset window has elapsed — transition to HalfOpen
                if (DateTime.UtcNow - _lastStateChange >= _resetTimeout)
                {
                    _state           = CircuitBreakerState.HalfOpen;
                    _lastStateChange = DateTime.UtcNow;
                    return true;
                }

                return false;
            }
        }

        /// <summary>Records a successful call. Closes circuit after enough consecutive successes in HalfOpen.</summary>
        public void RecordSuccess()
        {
            lock (_stateLock)
            {
                _consecutiveSuccesses++;

                if (_state == CircuitBreakerState.HalfOpen && _consecutiveSuccesses >= _successThreshold)
                {
                    _failureCount        = 0;
                    _consecutiveSuccesses = 0;
                    _state               = CircuitBreakerState.Closed;
                    _lastStateChange     = DateTime.UtcNow;
                }
                else if (_state == CircuitBreakerState.Closed)
                {
                    _failureCount = 0;
                }
            }
        }

        /// <summary>
        /// Records a failed call. Critical/High severity failures accumulate extra weight,
        /// accelerating the transition to Open.
        /// </summary>
        public void RecordFailure(ProxyErrorSeverity severity = ProxyErrorSeverity.Medium)
        {
            lock (_stateLock)
            {
                _consecutiveSuccesses = 0;

                // Severity-weighted increment: critical errors immediately breach threshold
                int increment = severity switch
                {
                    ProxyErrorSeverity.Critical => _failureThreshold,
                    ProxyErrorSeverity.High     => 2,
                    _                           => 1
                };

                _failureCount += increment;

                if (_state == CircuitBreakerState.Closed && _failureCount >= _failureThreshold)
                {
                    _state           = CircuitBreakerState.Open;
                    _lastStateChange = DateTime.UtcNow;
                }
                else if (_state == CircuitBreakerState.HalfOpen)
                {
                    // Failed probe: revert to Open and restart the reset timer
                    _state           = CircuitBreakerState.Open;
                    _lastStateChange = DateTime.UtcNow;
                }
            }
        }

        /// <summary>Forces the circuit to Closed state. Use only for manual recovery or tests.</summary>
        public void Reset()
        {
            lock (_stateLock)
            {
                _failureCount        = 0;
                _consecutiveSuccesses = 0;
                _state               = CircuitBreakerState.Closed;
                _lastStateChange     = DateTime.UtcNow;
            }
        }
    }

    public enum CircuitBreakerState
    {
        Closed,   // Normal operation — requests pass through
        Open,     // Failing — requests are rejected immediately
        HalfOpen  // Recovery probe — limited requests allowed to test health
    }
}

