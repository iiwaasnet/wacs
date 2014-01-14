using wacs.Messaging.Messages;
using wacs.Messaging.Messages.Intercom.Rsm;
using wacs.Rsm.Interface;

namespace wacs.Rsm.Implementation
{
    public partial class Acceptor : IAcceptor
    {
        private IMessage CreateNackPrepareNotLeaderMessage(IConsensusDecisionPayload payload)
        {
            return new RsmNackPrepareNotLeader(nodeResolver.ResolveLocalNode(),
                                               new RsmNackPrepareNotLeader.Payload
                                               {
                                                   Proposal = payload.Proposal,
                                                   LogIndex = payload.LogIndex
                                               });
        }

        private IMessage CreateNackPrepareAlreadyChosenMessage(IConsensusDecisionPayload payload)
        {
            return new RsmNackPrepareChosen(nodeResolver.ResolveLocalNode(),
                                            new RsmNackPrepareChosen.Payload
                                            {
                                                Proposal = payload.Proposal,
                                                LogIndex = payload.LogIndex
                                            });
        }

        private IMessage CreateNackPrepareMessage(IConsensusDecisionPayload payload)
        {
            return new RsmNackPrepareBlocked(nodeResolver.ResolveLocalNode(),
                                             new RsmNackPrepareBlocked.Payload
                                             {
                                                 Proposal = payload.Proposal,
                                                 LogIndex = payload.LogIndex,
                                                 MinProposal = new Messaging.Messages.Intercom.Rsm.Ballot
                                                               {
                                                                   ProposalNumber = acceptedProposal.ProposalNumber
                                                               }
                                             });
        }

        private IMessage CreateAckPrepareMessage(IConsensusDecisionPayload payload, ILogEntry logEntry)
        {
            return new RsmAckPrepare(nodeResolver.ResolveLocalNode(),
                                     new RsmAckPrepare.Payload
                                     {
                                         Proposal = payload.Proposal,
                                         LogIndex = payload.LogIndex,
                                         AcceptedValue = (logEntry != null && logEntry.State == LogEntryState.Accepted)
                                                             ? new Message(logEntry.Value.Envelope, logEntry.Value.Body)
                                                             : null,
                                         AcceptedProposal = (acceptedProposal != null)
                                                                ? new Messaging.Messages.Intercom.Rsm.Ballot {ProposalNumber = acceptedProposal.ProposalNumber}
                                                                : null
                                     });
        }

        private IMessage CreateNackAcceptNotLeaderMessage(RsmAccept.Payload payload)
        {
            return new RsmNackAcceptNotLeader(nodeResolver.ResolveLocalNode(),
                                              new RsmNackAcceptNotLeader.Payload
                                              {
                                                  Proposal = payload.Proposal,
                                                  LogIndex = payload.LogIndex
                                              });
        }

        private IMessage CreateNackAcceptAlreadyChosenMessage(RsmAccept.Payload payload)
        {
            return new RsmNackAcceptChosen(nodeResolver.ResolveLocalNode(),
                                           new RsmNackAcceptChosen.Payload
                                           {
                                               Proposal = payload.Proposal,
                                               LogIndex = payload.LogIndex
                                           });
        }

        private IMessage CreateNackAcceptMessage(RsmAccept.Payload payload)
        {
            return new RsmNackAcceptBlocked(nodeResolver.ResolveLocalNode(),
                                            new RsmNackAcceptBlocked.Payload
                                            {
                                                Proposal = payload.Proposal,
                                                LogIndex = payload.LogIndex,
                                                MinProposal = new Messaging.Messages.Intercom.Rsm.Ballot
                                                              {
                                                                  ProposalNumber = acceptedProposal.ProposalNumber
                                                              }
                                            });
        }

        private IMessage CreateAckAcceptMessage(RsmAccept.Payload payload)
        {
            return new RsmAckAccept(nodeResolver.ResolveLocalNode(),
                                    new RsmAckAccept.Payload
                                    {
                                        Proposal = payload.Proposal,
                                        LogIndex = payload.LogIndex
                                    });
        }
    }
}