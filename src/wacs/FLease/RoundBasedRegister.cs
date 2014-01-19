using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using wacs.Communication.Hubs.Intercom;
using wacs.Configuration;
using wacs.Diagnostics;
using wacs.Messaging.Messages;
using wacs.Messaging.Messages.Intercom.Lease;
using wacs.Resolver;

namespace wacs.FLease
{
    public partial class RoundBasedRegister : IRoundBasedRegister
    {
        private readonly IProcess owner;
        private readonly IIntercomMessageHub intercomMessageHub;
        private Ballot readBallot;
        private Ballot writeBallot;
        private ILease lease;
        private readonly IListener listener;
        private readonly ISynodConfigurationProvider synodConfigurationProvider;
        private readonly ILeaseConfiguration leaseConfig;
        private readonly ILogger logger;

        private readonly IObservable<IMessage> ackReadStream;
        private readonly IObservable<IMessage> nackReadStream;
        private readonly IObservable<IMessage> ackWriteStream;
        private readonly IObservable<IMessage> nackWriteStream;
        private readonly INodeResolver nodeResolver;

        public RoundBasedRegister(IIntercomMessageHub intercomMessageHub,
                                  IBallotGenerator ballotGenerator,
                                  ISynodConfigurationProvider synodConfigurationProvider,
                                  ILeaseConfiguration leaseConfig,
                                  INodeResolver nodeResolver,
                                  ILogger logger)
        {
            this.logger = logger;
            this.synodConfigurationProvider = synodConfigurationProvider;
            this.leaseConfig = leaseConfig;
            this.intercomMessageHub = intercomMessageHub;
            readBallot = (Ballot) ballotGenerator.Null();
            writeBallot = (Ballot) ballotGenerator.Null();
            owner = nodeResolver.ResolveLocalNode();
            this.nodeResolver = nodeResolver;

            listener = intercomMessageHub.Subscribe();

            listener.Where(m => m.Body.MessageType == LeaseRead.MessageType)
                    .Subscribe(new MessageStreamListener(OnReadReceived));
            listener.Where(m => m.Body.MessageType == LeaseWrite.MessageType)
                    .Subscribe(new MessageStreamListener(OnWriteReceived));

            ackReadStream = listener.Where(m => m.Body.MessageType == LeaseAckRead.MessageType);
            nackReadStream = listener.Where(m => m.Body.MessageType == LeaseNackRead.MessageType);
            ackWriteStream = listener.Where(m => m.Body.MessageType == LeaseAckWrite.MessageType);
            nackWriteStream = listener.Where(m => m.Body.MessageType == LeaseNackWrite.MessageType);

            listener.Start();
        }

        private void OnWriteReceived(IMessage message)
        {
            var payload = new LeaseWrite(message).GetPayload();

            var ballot = new Ballot(new DateTime(payload.Ballot.Timestamp, DateTimeKind.Utc),
                                    payload.Ballot.MessageNumber,
                                    new Process(payload.Ballot.ProcessId));
            IMessage response;
            if (writeBallot > ballot || readBallot > ballot)
            {
                LogNackWrite(ballot);

                response = new LeaseNackWrite(owner, new LeaseNackWrite.Payload {Ballot = payload.Ballot});
            }
            else
            {
                LogAckWrite(ballot);

                writeBallot = ballot;
                lease = new Lease(new Process(payload.Lease.ProcessId),
                                  new DateTime(payload.Lease.ExpiresAt, DateTimeKind.Utc));

                response = new LeaseAckWrite(owner, new LeaseAckWrite.Payload {Ballot = payload.Ballot});
            }
            intercomMessageHub.Send(message.Envelope.Sender, response);
        }

        private void OnReadReceived(IMessage message)
        {
            var payload = new LeaseRead(message).GetPayload();

            var ballot = new Ballot(new DateTime(payload.Ballot.Timestamp, DateTimeKind.Utc),
                                    payload.Ballot.MessageNumber,
                                    new Process(payload.Ballot.ProcessId));

            IMessage response;
            if (writeBallot >= ballot || readBallot >= ballot)
            {
                LogNackRead(ballot);

                response = new LeaseNackRead(owner, new LeaseNackRead.Payload {Ballot = payload.Ballot});
            }
            else
            {
                LogAckRead(ballot);

                readBallot = ballot;
                response = CreateLeaseAckReadMessage(payload);
            }

            intercomMessageHub.Send(message.Envelope.Sender, response);
        }

        public ILeaseTxResult Read(IBallot ballot)
        {
            var ackFilter = new LeaderElectionMessageFilter(ballot, (m) => new LeaseAckRead(m).GetPayload(), nodeResolver, synodConfigurationProvider);
            var nackFilter = new LeaderElectionMessageFilter(ballot, (m) => new LeaseNackRead(m).GetPayload(), nodeResolver, synodConfigurationProvider);

            var awaitableAckFilter = new AwaitableMessageStreamFilter(ackFilter.Match, GetQuorum());
            var awaitableNackFilter = new AwaitableMessageStreamFilter(nackFilter.Match, GetQuorum());

            using (ackReadStream.Subscribe(awaitableAckFilter))
            {
                using (nackReadStream.Subscribe(awaitableNackFilter))
                {
                    var message = CreateReadMessage(ballot);
                    intercomMessageHub.Broadcast(message);

                    var index = WaitHandle.WaitAny(new[] {awaitableAckFilter.Filtered, awaitableNackFilter.Filtered},
                                                   leaseConfig.NodeResponseTimeout);

                    if (ReadNotAcknowledged(index))
                    {
                        return new LeaseTxResult {TxOutcome = TxOutcome.Abort};
                    }

                    var lease = awaitableAckFilter
                        .MessageStream
                        .Select(m => new LeaseAckRead(m).GetPayload())
                        .Max(m => new LastWrittenLease(m.KnownWriteBallot, m.Lease))
                        .Lease;

                    return new LeaseTxResult
                           {
                               TxOutcome = TxOutcome.Commit,
                               Lease = lease
                           };
                }
            }
        }

        public ILeaseTxResult Write(IBallot ballot, ILease lease)
        {
            var ackFilter = new LeaderElectionMessageFilter(ballot, (m) => new LeaseAckWrite(m).GetPayload(), nodeResolver, synodConfigurationProvider);
            var nackFilter = new LeaderElectionMessageFilter(ballot, (m) => new LeaseNackWrite(m).GetPayload(), nodeResolver, synodConfigurationProvider);

            var awaitableAckFilter = new AwaitableMessageStreamFilter(ackFilter.Match, GetQuorum());
            var awaitableNackFilter = new AwaitableMessageStreamFilter(nackFilter.Match, GetQuorum());

            using (ackWriteStream.Subscribe(awaitableAckFilter))
            {
                using (nackWriteStream.Subscribe(awaitableNackFilter))
                {
                    var message = CreateWriteMessage(ballot, lease);
                    intercomMessageHub.Broadcast(message);

                    var index = WaitHandle.WaitAny(new[] {awaitableAckFilter.Filtered, awaitableNackFilter.Filtered},
                                                   leaseConfig.NodeResponseTimeout);

                    if (ReadNotAcknowledged(index))
                    {
                        return new LeaseTxResult {TxOutcome = TxOutcome.Abort};
                    }

                    return new LeaseTxResult
                           {
                               TxOutcome = TxOutcome.Commit,
                               // NOTE: needed???
                               Lease = lease
                           };
                }
            }
        }

        private static bool ReadNotAcknowledged(int index)
        {
            return index == 1 || index == WaitHandle.WaitTimeout;
        }

        private int GetQuorum()
        {
            return synodConfigurationProvider.Synod.Count() / 2 + 1;
        }

        public void Dispose()
        {
            listener.Stop();
        }

        private IMessage CreateWriteMessage(IBallot ballot, ILease lease)
        {
            return new LeaseWrite(owner,
                                  new LeaseWrite.Payload
                                  {
                                      Ballot = new Messaging.Messages.Intercom.Lease.Ballot
                                               {
                                                   ProcessId = ballot.Process.Id,
                                                   Timestamp = ballot.Timestamp.Ticks,
                                                   MessageNumber = ballot.MessageNumber
                                               },
                                      Lease = new Messaging.Messages.Intercom.Lease.Lease
                                              {
                                                  ProcessId = lease.Owner.Id,
                                                  ExpiresAt = lease.ExpiresAt.Ticks
                                              }
                                  });
        }

        private Message CreateReadMessage(IBallot ballot)
        {
            return new LeaseRead(owner,
                                 new LeaseRead.Payload
                                 {
                                     Ballot = new Messaging.Messages.Intercom.Lease.Ballot
                                              {
                                                  ProcessId = ballot.Process.Id,
                                                  Timestamp = ballot.Timestamp.Ticks,
                                                  MessageNumber = ballot.MessageNumber
                                              }
                                 });
        }

        private IMessage CreateLeaseAckReadMessage(LeaseRead.Payload payload)
        {
            return new LeaseAckRead(owner,
                                    new LeaseAckRead.Payload
                                    {
                                        Ballot = payload.Ballot,
                                        KnownWriteBallot = new Messaging.Messages.Intercom.Lease.Ballot
                                                           {
                                                               ProcessId = writeBallot.Process.Id,
                                                               Timestamp = writeBallot.Timestamp.Ticks,
                                                               MessageNumber = writeBallot.MessageNumber
                                                           },
                                        Lease = (lease != null)
                                                    ? new Messaging.Messages.Intercom.Lease.Lease
                                                      {
                                                          ProcessId = lease.Owner.Id,
                                                          ExpiresAt = lease.ExpiresAt.Ticks
                                                      }
                                                    : null
                                    });
        }
    }
}