using System;
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
    public partial class Acceptor : IAcceptor
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
            try
            {
                var payload = new RsmPrepare(message).GetPayload();
                var proposal = new Ballot(payload.Proposal.ProposalNumber);

                IMessage response;

                if (PrepareCameNotFromLeader(message.Envelope.Sender))
                {
                    response = CreateNackNotLeaderMessage(payload);
                }
                else
                {
                    lock (locker)
                    {
                        response = RespondOnPrepareRequestFromLeader(payload, proposal);
                    }
                }

                intercomMessageHub.Send(new Process(message.Envelope.Sender.Id), response);
            }
            catch (Exception err)
            {
                logger.Error(err);
            }
        }
    }
}