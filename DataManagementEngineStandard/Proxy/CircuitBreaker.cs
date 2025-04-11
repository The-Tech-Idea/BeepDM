using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Proxy
{
    // Define a dedicated circuit breaker class
    public class CircuitBreaker
    {
        private readonly object _stateLock = new object();
        private CircuitState _state = CircuitState.Closed;
        private DateTime _lastStateChange = DateTime.UtcNow;
        private int _failureCount;
        private readonly int _failureThreshold;
        private readonly TimeSpan _resetTimeout;

        public CircuitBreaker(int failureThreshold, TimeSpan resetTimeout)
        {
            _failureThreshold = failureThreshold;
            _resetTimeout = resetTimeout;
        }

        public bool CanExecute()
        {
            lock (_stateLock)
            {
                // Allow execution when circuit is closed
                if (_state == CircuitState.Closed)
                    return true;

                // If in half-open state, allow a test request
                if (_state == CircuitState.HalfOpen)
                    return true;

                // If reset timeout has elapsed, transition to half-open
                if (_state == CircuitState.Open &&
                    DateTime.UtcNow - _lastStateChange >= _resetTimeout)
                {
                    _state = CircuitState.HalfOpen;
                    _lastStateChange = DateTime.UtcNow;
                    return true;
                }

                return false;
            }
        }

        public void RecordSuccess()
        {
            lock (_stateLock)
            {
                _failureCount = 0;
                _state = CircuitState.Closed;
                _lastStateChange = DateTime.UtcNow;
            }
        }

        public void RecordFailure()
        {
            lock (_stateLock)
            {
                _failureCount++;

                // Transition to open state if threshold reached
                if (_state == CircuitState.Closed && _failureCount >= _failureThreshold)
                {
                    _state = CircuitState.Open;
                    _lastStateChange = DateTime.UtcNow;
                }
                else if (_state == CircuitState.HalfOpen)
                {
                    // Return to open state if test request failed
                    _state = CircuitState.Open;
                    _lastStateChange = DateTime.UtcNow;
                }
            }
        }

        private enum CircuitState
        {
            Closed,   // Normal operation
            Open,     // Failing - rejecting requests
            HalfOpen  // Testing if system has recovered
        }
    }

}
