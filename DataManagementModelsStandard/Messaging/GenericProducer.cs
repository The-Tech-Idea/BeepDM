using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TheTechIdea.Beep.Messaging
{
    public interface IGenericProducer<TMessage> where TMessage : class
    {
        Task SendAsync(TMessage message, CancellationToken cancellationToken);
    }

    public class GenericProducer<TMessage> : IGenericProducer<TMessage> where TMessage : class
    {
        private readonly Func<TMessage, CancellationToken, Task> _sendAction;
        private readonly Action<Exception> _errorHandler;

        /// <summary>
        /// Creates a generic producer with a send action.
        /// </summary>
        /// <param name="sendAction">Delegate to send messages.</param>
        /// <param name="errorHandler">Optional delegate to handle exceptions.</param>
        public GenericProducer(
            Func<TMessage, CancellationToken, Task> sendAction,
            Action<Exception> errorHandler = null)
        {
            _sendAction = sendAction ?? throw new ArgumentNullException(nameof(sendAction));
            _errorHandler = errorHandler;
        }

        public async Task SendAsync(TMessage message, CancellationToken cancellationToken)
        {
            try
            {
                await _sendAction(message, cancellationToken);
            }
            catch (Exception ex)
            {
                _errorHandler?.Invoke(ex);
                throw;
            }
        }
    }
}
