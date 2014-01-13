using wacs.Messaging.Messages;
using wacs.Messaging.Messages.Intercom.Rsm;
using wacs.Rsm.Interface;

namespace wacs.Rsm.Implementation
{
    public partial class Acceptor : IAcceptor
    {
        private IMessage CreateNackNotLeaderMessage(IPreparePayload payload)
        {
            return new RsmNackPrepareNotLeader(nodeResolver.ResolveLocalNode(),
                                               new RsmNackPrepareNotLeader.Payload
                                               {
                                                   Proposal = payload.Proposal,
                                                   LogIndex = payload.LogIndex
                                               });
        }

        private IMessage CreateNackChosenMessage(IPreparePayload payload)
        {
            return new RsmNackPrepareChosen(nodeResolver.ResolveLocalNode(),
                                            new RsmNackPrepareChosen.Payload
                                            {
                                                Proposal = payload.Proposal,
                                                LogIndex = payload.LogIndex
                                            });
        }

        private IMessage CreateNackPrepareMessage(IPreparePayload payload)
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

        private IMessage CreateAckPrepareMessage(IPreparePayload payload, ILogEntry logEntry)
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