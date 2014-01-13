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

        private IMessage RespondOnPrepareRequestFromLeader(RsmPrepare.Payload payload, Ballot proposal)
        {
            IMessage response;

            var logEntry = replicatedLog.GetLogEntry(new LogIndex(payload.LogIndex.Index));

            if (logEntry.State == LogEntryState.Chosen)
            {
                response = CreateNackChosenMessage(payload);
            }
            else
            {
                response = RespondOnUnchosenLogEntry(payload, proposal, logEntry);
            }
            return response;
        }

        private IMessage RespondOnUnchosenLogEntry(RsmPrepare.Payload payload, Ballot proposal, ILogEntry logEntry)
        {
            IMessage response;

            if (proposal > minProposal)
            {
                minProposal = proposal;
                response = CreateAckPrepareMessage(payload, logEntry);
            }
            else
            {
                response = CreateNackPrepareMessage(payload);
            }

            return response;
        }

        private IMessage CreateNackChosenMessage(RsmPrepare.Payload payload)
        {
            return new RsmNackPrepareChosen(nodeResolver.ResolveLocalNode(),
                                            new RsmNackPrepareChosen.Payload
                                            {
                                                Proposal = payload.Proposal,
                                                LogIndex = payload.LogIndex
                                            });
        }

        private IMessage CreateNackPrepareMessage(RsmPrepare.Payload payload)
        {
            return new RsmNackPrepareBlocked(nodeResolver.ResolveLocalNode(),
                                             new RsmNackPrepareBlocked.Payload
                                             {
                                                 Proposal = payload.Proposal,
                                                 LogIndex = payload.LogIndex,
                                                 AcceptedProposal = new Messaging.Messages.Intercom.Rsm.Ballot
                                                                    {
                                                                        ProposalNumber = acceptedProposal.ProposalNumber
                                                                    }
                                             });
        }

        private IMessage CreateNackNotLeaderMessage(RsmPrepare.Payload payload)
        {
            return new RsmNackPrepareNotLeader(nodeResolver.ResolveLocalNode(),
                                               new RsmNackPrepareNotLeader.Payload
                                               {
                                                   Proposal = payload.Proposal,
                                                   LogIndex = payload.LogIndex
                                               });
        }

        private bool PrepareCameNotFromLeader(IProcess sender)
        {
            return !sender.Equals(leaseProvider.GetLease().Result.Owner);
        }

        private IMessage CreateAckPrepareMessage(RsmPrepare.Payload payload, ILogEntry logEntry)
        {
            return new RsmAckPrepare(nodeResolver.ResolveLocalNode(),
                                     new RsmAckPrepare.Payload
                                     {
                                         Proposal = payload.Proposal,
                                         LogIndex = payload.LogIndex,
                                         AcceptedValue = (logEntry != null && logEntry.State == LogEntryState.Accepted)
                                                             ? new Message(logEntry.Value.Command.Envelope, logEntry.Value.Command.Body)
                                                             : null,
                                         AcceptedProposal = (acceptedProposal != null)
                                                                ? new Messaging.Messages.Intercom.Rsm.Ballot {ProposalNumber = acceptedProposal.ProposalNumber}
                                                                : null
                                     });
        }
    }
}