using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using wacs.Configuration;
using wacs.Diagnostics;
using wacs.FLease.Messages;
using wacs.Messaging;
using wacs.Resolver.Interface;

namespace wacs.FLease
{
    public partial class RoundBasedRegister : IRoundBasedRegister
    {
        private readonly IProcess owner;
        private readonly IMessageHub messageHub;
        private Ballot readBallot;
        private Ballot writeBallot;
        private ILease lease;
        private readonly IListener listener;
        private readonly ITopologyConfiguration topology;
        private readonly ILeaseConfiguration leaseConfig;
        private readonly ILogger logger;

        private readonly IObservable<IMessage> ackReadStream;
        private readonly IObservable<IMessage> nackReadStream;
        private readonly IObservable<IMessage> ackWriteStream;
        private readonly IObservable<IMessage> nackWriteStream;
        private readonly INodeResolver nodeResolver;

        public RoundBasedRegister(IProcess owner,
                                  IMessageHub messageHub,
                                  IBallotGenerator ballotGenerator,
                                  ITopologyConfiguration topology,
                                  ILeaseConfiguration leaseConfig,
                                  INodeResolver nodeResolver,
                                  ILogger logger)
        {
            this.logger = logger;
            this.topology = topology;
            this.leaseConfig = leaseConfig;
            this.messageHub = messageHub;
            readBallot = (Ballot) ballotGenerator.Null();
            writeBallot = (Ballot) ballotGenerator.Null();
            this.owner = owner;
            this.nodeResolver = nodeResolver;

            listener = messageHub.Subscribe();

            listener.Where(m => m.Body.MessageType == LeaseRead.MessageType)
                    .Subscribe(new MessageStreamListener(OnReadReceived));
            listener.Where(m => m.Body.MessageType == LeaseWrite.MessageType)
                    .Subscribe(new MessageStreamListener(OnWriteReceived));

            ackReadStream = listener.Where(m => m.Body.MessageType == LeaseAckRead.MessageType);
            nackReadStream = listener.Where(m => m.Body.MessageType == LeaseNackRead.MessageType);
            ackWriteStream = listener.Where(m => m.Body.MessageType == LeaseAckWrite.MessageType);
            nackWriteStream = listener.Where(m => m.Body.MessageType == LeaseNackWrite.MessageType);
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
                response = new LeaseNackWrite(owner, new LeaseNackWrite.Payload {Ballot = payload.Ballot});
            }
            else
            {
                writeBallot = ballot;
                lease = new Lease(new Process(payload.Lease.ProcessId),
                                  new DateTime(payload.Lease.ExpiresAt, DateTimeKind.Utc));

                response = new LeaseAckWrite(owner, new LeaseAckWrite.Payload {Ballot = payload.Ballot});
            }
            messageHub.Send(message.Envelope.Sender, response);
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
                readBallot = ballot;
                response = CreateLeaseAckReadMessage(payload);
            }

            messageHub.Send(message.Envelope.Sender, response);
        }

        public ILeaseTxResult Read(IBallot ballot)
        {
            var ackFilter = new LeaderElectionMessageFilter(ballot, LeaseAckRead.MessageType, (m) => new LeaseAckRead(m).GetPayload(), nodeResolver, topology.Synod);
            var nackFilter = new LeaderElectionMessageFilter(ballot, LeaseNackRead.MessageType, (m) => new LeaseNackRead(m).GetPayload(), nodeResolver, topology.Synod);

            var ackReadFilter = new AwaitableMessageStreamFilter(ackFilter.Match, GetQuorum());
            var nackReadFilter = new AwaitableMessageStreamFilter(nackFilter.Match, GetQuorum());

            using (ackReadStream.Subscribe(ackReadFilter))
            {
                using (nackReadStream.Subscribe(nackReadFilter))
                {
                    var message = CreateReadMessage(ballot);
                    messageHub.Broadcast(message);

                    var index = WaitHandle.WaitAny(new[] {ackReadFilter.Filtered, nackReadFilter.Filtered},
                                                   leaseConfig.NodeResponseTimeout);

                    if (ReadNotAcknowledged(index))
                    {
                        return new LeaseTxResult {TxOutcome = TxOutcome.Abort};
                    }

                    var lease = ackReadFilter
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
            var ackFilter = new LeaderElectionMessageFilter(ballot, LeaseAckWrite.MessageType, (m) => new LeaseAckWrite(m).GetPayload(), nodeResolver, topology.Synod);
            var nackFilter = new LeaderElectionMessageFilter(ballot, LeaseNackWrite.MessageType, (m) => new LeaseNackWrite(m).GetPayload(), nodeResolver, topology.Synod);

            var ackWriteFilter = new AwaitableMessageStreamFilter(ackFilter.Match, GetQuorum());
            var nackWriteFilter = new AwaitableMessageStreamFilter(nackFilter.Match, GetQuorum());

            using (ackWriteStream.Subscribe(ackWriteFilter))
            {
                using (nackWriteStream.Subscribe(nackWriteFilter))
                {
                    var message = CreateWriteMessage(ballot, lease);
                    messageHub.Broadcast(message);

                    var index = WaitHandle.WaitAny(new[] {ackWriteFilter.Filtered, nackWriteFilter.Filtered},
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
            return topology.Synod.Members.Count() / 2 + 1;
        }

        public void Start()
        {
            listener.Start();
        }

        public void Stop()
        {
            listener.Stop();
        }

        private IMessage CreateWriteMessage(IBallot ballot, ILease lease)
        {
            return new LeaseWrite(owner,
                                  new LeaseWrite.Payload
                                  {
                                      Ballot = new Messages.Ballot
                                               {
                                                   ProcessId = ballot.Process.Id,
                                                   Timestamp = ballot.Timestamp.Ticks,
                                                   MessageNumber = ballot.MessageNumber
                                               },
                                      Lease = new Messages.Lease
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
                                     Ballot = new Messages.Ballot
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
                                        KnownWriteBallot = new Messages.Ballot
                                                           {
                                                               ProcessId = writeBallot.Process.Id,
                                                               Timestamp = writeBallot.Timestamp.Ticks,
                                                               MessageNumber = writeBallot.MessageNumber
                                                           },
                                        Lease = (lease != null)
                                                    ? new Messages.Lease
                                                      {
                                                          ProcessId = lease.Owner.Id,
                                                          ExpiresAt = lease.ExpiresAt.Ticks
                                                      }
                                                    : null
                                    });
        }
    }
}