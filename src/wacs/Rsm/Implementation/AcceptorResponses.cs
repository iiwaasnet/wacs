using wacs.Messaging.Messages;
using wacs.Messaging.Messages.Intercom.Rsm;
using wacs.Rsm.Interface;

namespace wacs.Rsm.Implementation
{
    public partial class Acceptor
    {
        private IMessage CreateNackPrepareNotLeaderMessage(IConsensusDecisionPayload payload)
        {
            return new RsmNackPrepareNotLeader(new Process {Id = nodeResolver.ResolveLocalNode().Id},
                                               new RsmNackPrepareNotLeader.Payload
                                               {
                                                   Proposal = payload.Proposal,
                                                   LogIndex = payload.LogIndex
                                               });
        }

        private IMessage CreateNackPrepareAlreadyChosenMessage(IConsensusDecisionPayload payload)
        {
            return new RsmNackPrepareChosen(new Process {Id = nodeResolver.ResolveLocalNode().Id},
                                            new RsmNackPrepareChosen.Payload
                                            {
                                                Proposal = payload.Proposal,
                                                LogIndex = payload.LogIndex
                                            });
        }

        private IMessage CreateNackPrepareMessage(IConsensusDecisionPayload payload)
        {
            return new RsmNackPrepareBlocked(new Process {Id = nodeResolver.ResolveLocalNode().Id},
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
            return new RsmAckPrepare(new Process {Id = nodeResolver.ResolveLocalNode().Id},
                                     new RsmAckPrepare.Payload
                                     {
                                         Proposal = payload.Proposal,
                                         LogIndex = payload.LogIndex,
                                         AcceptedValue = (logEntry != null && logEntry.State == LogEntryState.Accepted)
                                                             ? new Message(logEntry.Command.Request.Envelope, logEntry.Command.Request.Body)
                                                             : null,
                                         AcceptedProposal = (acceptedProposal != null)
                                                                ? new Messaging.Messages.Intercom.Rsm.Ballot {ProposalNumber = acceptedProposal.ProposalNumber}
                                                                : null
                                     });
        }

        private IMessage CreateNackAcceptNotLeaderMessage(RsmAccept.Payload payload)
        {
            return new RsmNackAcceptNotLeader(new Process {Id = nodeResolver.ResolveLocalNode().Id},
                                              new RsmNackAcceptNotLeader.Payload
                                              {
                                                  Proposal = payload.Proposal,
                                                  LogIndex = payload.LogIndex
                                              });
        }

        private IMessage CreateNackAcceptAlreadyChosenMessage(RsmAccept.Payload payload)
        {
            return new RsmNackAcceptChosen(new Process {Id = nodeResolver.ResolveLocalNode().Id},
                                           new RsmNackAcceptChosen.Payload
                                           {
                                               Proposal = payload.Proposal,
                                               LogIndex = payload.LogIndex
                                           });
        }

        private IMessage CreateNackAcceptMessage(RsmAccept.Payload payload)
        {
            return new RsmNackAcceptBlocked(new Process {Id = nodeResolver.ResolveLocalNode().Id},
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
            return new RsmAckAccept(new Process {Id = nodeResolver.ResolveLocalNode().Id},
                                    new RsmAckAccept.Payload
                                    {
                                        Proposal = payload.Proposal,
                                        LogIndex = payload.LogIndex
                                    });
        }
    }
}