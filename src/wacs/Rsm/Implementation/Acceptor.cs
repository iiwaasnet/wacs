using System.Reactive.Linq;
using wacs.Diagnostics;
using wacs.FLease;
using wacs.Messaging.Hubs.Intercom;
using wacs.Messaging.Messages;
using wacs.Messaging.Messages.Intercom.Rsm;
using wacs.Rsm.Interface;

namespace wacs.Rsm.Implementation
{
    public class Acceptor : IAcceptor
    {
        private readonly ILogger logger;
        private readonly IListener listener;

        public Acceptor(IIntercomMessageHub intercomMessageHub, ILogger logger)
        {
            this.logger = logger;

            listener = intercomMessageHub.Subscribe();

            listener.Where(m => m.Body.MessageType == PrepareMessage.MessageType)
                    .Subscribe(new MessageStreamListener(OnPrepareReceived));
        }

        private void OnPrepareReceived(IMessage obj)
        {
            throw new System.NotImplementedException();
        }
    }
}