using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Messaging
{
    public interface IGenericConsumer<TMessage> where TMessage : class
    {
        Task ConsumeAsync(TMessage message, CancellationToken cancellationToken);
        public class GenericConsumer<TMessage> : IGenericConsumer<TMessage> where TMessage : class
        {
            private readonly Func<TMessage, CancellationToken, Task> _handler;
            private readonly Action<Exception> _errorHandler;

            /// <summary>
            /// Creates a generic consumer with a message handler.
            /// </summary>
            /// <param name="handler">Delegate to process messages.</param>
            /// <param name="errorHandler">Optional delegate to handle exceptions.</param>
            public GenericConsumer(
                Func<TMessage, CancellationToken, Task> handler,
                Action<Exception> errorHandler = null)
            {
                _handler = handler ?? throw new ArgumentNullException(nameof(handler));
                _errorHandler = errorHandler;
            }

            public async Task ConsumeAsync(TMessage message, CancellationToken cancellationToken)
            {
                try
                {
                    await _handler(message, cancellationToken);
                }
                catch (Exception ex)
                {
                    _errorHandler?.Invoke(ex);
                    throw; // rethrow so that any messaging framework can also handle retries or logging
                }
            }
        }
    }
   
}
