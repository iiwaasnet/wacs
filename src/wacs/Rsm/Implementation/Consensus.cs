using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using wacs.Configuration;
using wacs.FLease;
using wacs.Messaging.Hubs.Intercom;
using wacs.Messaging.Messages;
using wacs.Messaging.Messages.Intercom.Rsm;
using wacs.Resolver;
using wacs.Rsm.Interface;
using IBallot = wacs.Rsm.Interface.IBallot;
using Process = wacs.Messaging.Messages.Intercom.Process;

namespace wacs.Rsm.Implementation
{
    public class Consensus : IConsensus
    {
        private readonly IConsensusRoundManager consensusRoundManager;
        private readonly IIntercomMessageHub intercomMessageHub;
        private readonly IListener listener;
        private readonly IObservable<IMessage> ackPrepareStream;
        private readonly IObservable<IMessage> nackPrepareStream;
        private readonly ISynodConfigurationProvider synodConfigurationProvider;
        private readonly INodeResolver nodeResolver;
        private readonly IRsmConfiguration rsmConfig;

        public Consensus(IConsensusRoundManager consensusRoundManager,
                         IIntercomMessageHub intercomMessageHub,
                         ISynodConfigurationProvider synodConfigurationProvider,
                         INodeResolver nodeResolver,
                         IRsmConfiguration rsmConfig)
        {
            this.consensusRoundManager = consensusRoundManager;
            this.intercomMessageHub = intercomMessageHub;
            this.synodConfigurationProvider = synodConfigurationProvider;
            this.nodeResolver = nodeResolver;
            this.rsmConfig = rsmConfig;
            listener = intercomMessageHub.Subscribe();

            ackPrepareStream = listener.Where(m => m.Body.MessageType == RsmAckPrepare.MessageType);
            nackPrepareStream = listener.Where(m => m.Body.MessageType == RsmNackPrepareBlocked.MessageType
                                                    || m.Body.MessageType == RsmNackPrepareChosen.MessageType);
        }

        public IDecision Decide(ILogIndex index, IMessage command, bool fast)
        {
            var ballot = consensusRoundManager.GetNextBallot();
            SendPrepare(index, ballot, command);

            return null;
        }

        private void SendPrepare(ILogIndex logIndex, IBallot ballot, IMessage command)
        {
            var ackFilter = new RsmPrepareAckMessageFilter(ballot, logIndex, nodeResolver, synodConfigurationProvider);
            var nackFilter = new RsmPrepareNackMessageFilter(ballot, logIndex, nodeResolver, synodConfigurationProvider);

            var awaitableAckFilter = new AwaitableMessageStreamFilter(ackFilter.Match, GetQuorum());
            var awaitableNackFilter = new AwaitableMessageStreamFilter(nackFilter.Match, GetQuorum());

            using (ackPrepareStream.Subscribe(awaitableAckFilter))
            {
                using (nackPrepareStream.Subscribe(awaitableNackFilter))
                {
                    var message = CreatePrepareMessage(logIndex, ballot);
                    intercomMessageHub.Broadcast(message);

                    var index = WaitHandle.WaitAny(new[] {awaitableAckFilter.Filtered, awaitableNackFilter.Filtered}, rsmConfig.CommandExecutionTimeout);
                }
            }
        }

        private IMessage CreatePrepareMessage(ILogIndex index, IBallot ballot)
        {
            return new RsmPrepare(synodConfigurationProvider.LocalProcess,
                                  new RsmPrepare.Payload
                                  {
                                      Ballot = new Messaging.Messages.Intercom.Rsm.Ballot {ProposalNumber = ballot.ProposalNumber},
                                      LogIndex = new LogIndex {Index = index.Index},
                                      Leader = new Process {Id = synodConfigurationProvider.LocalProcess.Id}
                                  });
        }

        private int GetQuorum()
        {
            return synodConfigurationProvider.Synod.Count() / 2 + 1;
        }
    }
}