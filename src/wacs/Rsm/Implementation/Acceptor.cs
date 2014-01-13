using System.Reactive.Linq;
using wacs.Configuration;
using wacs.Diagnostics;
using wacs.FLease;
using wacs.Messaging.Hubs.Intercom;
using wacs.Messaging.Messages;
using wacs.Messaging.Messages.Intercom.Rsm;
using wacs.Resolver;
using wacs.Rsm.Interface;

namespace wacs.Rsm.Implementation
{
    public class Acceptor : IAcceptor
    {
        private readonly ILogger logger;
        private readonly IListener listener;
        private readonly IReplicatedLog replicatedLog;
        private Ballot minProposal;
        private Ballot acceptedProposal;
        private readonly IIntercomMessageHub intercomMessageHub;
        private readonly ILeaseProvider leaseProvider;
        private readonly INodeResolver nodeResolver;
        private readonly object locker = new object();

        public Acceptor(IIntercomMessageHub intercomMessageHub,
                        IReplicatedLog replicatedLog,
                        ILeaseProvider leaseProvider,
                        INodeResolver nodeResolver,
                        ILogger logger)
        {
            this.logger = logger;
            this.replicatedLog = replicatedLog;
            this.intercomMessageHub = intercomMessageHub;
            this.nodeResolver = nodeResolver;
            this.leaseProvider = leaseProvider;

            listener = intercomMessageHub.Subscribe();

            listener.Where(m => m.Body.MessageType == RsmPrepare.MessageType)
                    .Subscribe(new MessageStreamListener(OnPrepareReceived));
        }

        private void OnPrepareReceived(IMessage message)
        {
            var payload = new RsmPrepare(message).GetPayload();
            var proposal = new Ballot(payload.Ballot.ProposalNumber);

            IMessage response;

            lock (locker)
            {
                if (PrepareCameNotFromLeader(message.Envelope.Sender))
                {
                    response = CreateNackNotLeaderMessage(payload);
                }
                else
                {
                    if (proposal > minProposal)
                    {
                        minProposal = proposal;
                        response = CreateAckPrepareMessage(payload);
                    }
                }
            }

            intercomMessageHub.Send(new Process(message.Envelope.Sender.Id), response);
        }

        private IMessage CreateNackNotLeaderMessage(RsmPrepare.Payload payload)
        {
            return new RsmNackPrepareNotLeader(nodeResolver.ResolveLocalNode(),
                                               new RsmNackPrepareNotLeader.Payload
                                               {
                                                   Ballot = payload.Ballot,
                                                   LogIndex = payload.LogIndex
                                               });
        }

        private bool PrepareCameNotFromLeader(IProcess sender)
        {
            return !sender.Equals(leaseProvider.GetLease().Result.Owner);
        }

        private IMessage CreateAckPrepareMessage(RsmPrepare.Payload payload)
        {
            return new RsmAckPrepare();
        }
    }
}