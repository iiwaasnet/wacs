﻿using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using wacs.Configuration;
using wacs.Diagnostics;
using wacs.FLease.Messages;
using wacs.Messaging;

namespace wacs.FLease
{
    public partial class RoundBasedRegister : IRoundBasedRegister
    {
        private readonly INode owner;
        private readonly IMessageHub messageHub;
        private Ballot readBallot;
        private Ballot writeBallot;
        private ILease lease;
        private readonly IListener listener;
        private readonly ISynodConfiguration synodConfig;
        private readonly ILeaseConfiguration leaseConfig;
        private readonly ILogger logger;

        private readonly IObservable<IMessage> ackReadStream;
        private readonly IObservable<IMessage> nackReadStream;
        private readonly IObservable<IMessage> ackWriteStream;
        private readonly IObservable<IMessage> nackWriteStream;
        private readonly IMessageSerializer serializer;

        public RoundBasedRegister(INode owner,
                                  IMessageHub messageHub,
                                  IBallotGenerator ballotGenerator,
                                  IMessageSerializer serializer,
                                  ISynodConfiguration synodConfig,
                                  ILeaseConfiguration leaseConfig,
                                  ILogger logger)
        {
            this.logger = logger;
            this.synodConfig = synodConfig;
            this.leaseConfig = leaseConfig;
            this.messageHub = messageHub;
            readBallot = (Ballot) ballotGenerator.Null();
            writeBallot = (Ballot) ballotGenerator.Null();
            this.serializer = serializer;
            this.owner = owner;

            listener = messageHub.Subscribe();

            listener.Where(m => m.Body.MessageType.ToMessageType() == FLeaseMessageType.Read)
                    .Subscribe(new MessageStreamListener(OnReadReceived));
            listener.Where(m => m.Body.MessageType.ToMessageType() == FLeaseMessageType.Write)
                    .Subscribe(new MessageStreamListener(OnWriteReceived));

            ackReadStream = listener.Where(m => m.Body.MessageType.ToMessageType() == FLeaseMessageType.AckRead);
            nackReadStream = listener.Where(m => m.Body.MessageType.ToMessageType() == FLeaseMessageType.NackRead);
            ackWriteStream = listener.Where(m => m.Body.MessageType.ToMessageType() == FLeaseMessageType.AckWrite);
            nackWriteStream = listener.Where(m => m.Body.MessageType.ToMessageType() == FLeaseMessageType.NackWrite);
        }

        private void OnWriteReceived(IMessage message)
        {
            var writeMessage = serializer.Deserialize<WritePayload>(message.Body.Content);
            var ballot = new Ballot(new DateTime(writeMessage.Ballot.Timestamp, DateTimeKind.Utc),
                                    writeMessage.Ballot.MessageNumber,
                                    new Node(writeMessage.Ballot.ProcessId));
            if (writeBallot > ballot || readBallot > ballot)
            {
                messageHub.Send(new Node(message.Envelope.Sender.Id),
                                new Message(
                                    new Envelope {Sender = owner},
                                    new Body
                                    {
                                        MessageType = FLeaseMessageType.NackWrite.ToMessageType(),
                                        Content = serializer.Serialize(new NackWritePayload {Ballot = writeMessage.Ballot})
                                    }));
            }
            else
            {
                writeBallot = ballot;
                lease = new Lease(new Node(writeMessage.Lease.ProcessId),
                                  new DateTime(writeMessage.Lease.ExpiresAt, DateTimeKind.Utc));

                var msg = new AckWritePayload {Ballot = writeMessage.Ballot};
                messageHub.Send(message.Envelope.Sender,
                                new Message(
                                    new Envelope {Sender = owner},
                                    new Body
                                    {
                                        MessageType = FLeaseMessageType.AckWrite.ToMessageType(),
                                        Content = serializer.Serialize(msg)
                                    }));
            }
        }

        private void OnReadReceived(IMessage message)
        {
            var readMessage = serializer.Deserialize<ReadPayload>(message.Body.Content);
            var ballot = new Ballot(new DateTime(readMessage.Ballot.Timestamp, DateTimeKind.Utc),
                                    readMessage.Ballot.MessageNumber,
                                    new Node(readMessage.Ballot.ProcessId));

            if (writeBallot >= ballot || readBallot >= ballot)
            {
                LogNackRead(ballot);

                messageHub.Send(new Node(message.Envelope.Sender.Id),
                                new Message(
                                    new Envelope {Sender = owner},
                                    new Body
                                    {
                                        MessageType = FLeaseMessageType.NackRead.ToMessageType(),
                                        Content = serializer.Serialize(new NackReadPayload {Ballot = readMessage.Ballot})
                                    }));
            }
            else
            {
                readBallot = ballot;
                var msg = new AckReadPayload
                          {
                              Ballot = readMessage.Ballot,
                              KnownWriteBallot = new Messages.Ballot
                                                 {
                                                     ProcessId = writeBallot.Node.Id,
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
                          };
                messageHub.Send(message.Envelope.Sender,
                                new Message(
                                    new Envelope {Sender = owner},
                                    new Body
                                    {
                                        MessageType = FLeaseMessageType.AckRead.ToMessageType(),
                                        Content = serializer.Serialize(msg)
                                    }));
            }
        }

        public ILeaseTxResult Read(IBallot ballot)
        {
            var ackReadFilter = new AwaitableMessageStreamFilter(m => FilterAckMessages<AckReadPayload>(ballot, m, FLeaseMessageType.AckRead), GetQuorum());
            var nackReadFilter = new AwaitableMessageStreamFilter(m => FilterAckMessages<NackReadPayload>(ballot, m, FLeaseMessageType.NackRead), GetQuorum());
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
                        .Select(m => serializer.Deserialize<AckReadPayload>(m.Body.Content))
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
            var ackWriteFilter = new AwaitableMessageStreamFilter(m => FilterAckMessages<AckWritePayload>(ballot, m, FLeaseMessageType.AckWrite), GetQuorum());
            var nackWriteFilter = new AwaitableMessageStreamFilter(m => FilterAckMessages<NackWritePayload>(ballot, m, FLeaseMessageType.NackWrite), GetQuorum());
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

        private bool FilterAckMessages<TPayload>(IBallot ballot, IMessage message, FLeaseMessageType msgType)
            where TPayload : IMessagePayload
        {
            if (message.Body.MessageType.ToMessageType() == msgType)
            {
                var ackRead = serializer.Deserialize<TPayload>(message.Body.Content);

                return ackRead.Ballot.ProcessId == ballot.Node.Id && ackRead.Ballot.Timestamp == ballot.Timestamp.Ticks;
            }

            return false;
        }

        private int GetQuorum()
        {
            return synodConfig.Nodes.Count() / 2 + 1;
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
            var message = new Message(
                new Envelope {Sender = owner},
                new Body
                {
                    MessageType = FLeaseMessageType.Write.ToMessageType(),
                    Content = serializer.Serialize(new WritePayload
                                                   {
                                                       Ballot = new Messages.Ballot
                                                                {
                                                                    ProcessId = ballot.Node.Id,
                                                                    Timestamp = ballot.Timestamp.Ticks,
                                                                    MessageNumber = ballot.MessageNumber
                                                                },
                                                       Lease = new Messages.Lease
                                                               {
                                                                   ProcessId = lease.Owner.Id,
                                                                   ExpiresAt = lease.ExpiresAt.Ticks
                                                               }
                                                   })
                });

            return message;
        }

        private Message CreateReadMessage(IBallot ballot)
        {
            var message = new Message(
                new Envelope {Sender = owner},
                new Body
                {
                    MessageType = FLeaseMessageType.Read.ToMessageType(),
                    Content = serializer.Serialize(new ReadPayload
                                                   {
                                                       Ballot = new Messages.Ballot
                                                                {
                                                                    ProcessId = ballot.Node.Id,
                                                                    Timestamp = ballot.Timestamp.Ticks,
                                                                    MessageNumber = ballot.MessageNumber
                                                                }
                                                   })
                });
            return message;
        }
    }
}