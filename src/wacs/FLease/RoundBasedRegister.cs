using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using wacs.FLease.Messages;
using wacs.Messaging;

namespace wacs.FLease
{
	public class RoundBasedRegister : IRoundBasedRegister
	{
		private IProcess owner;
		private readonly IMessageHub messageHub;
		private Ballot readBallot;
		private Ballot writeBallot;
		private ILease lease;
		private IListener listener;
		private readonly IWacsConfiguration config;

		private IObservable<IMessage> ackReadStream;
		private IObservable<IMessage> nackReadStream;
		private IObservable<IMessage> ackWriteStream;
		private IObservable<IMessage> nackWriteStream;
		private readonly IMessageSerializer serializer;

		public RoundBasedRegister(IMessageHub messageHub,
		                          IBallotGenerator ballotGenerator,
		                          IMessageSerializer serializer,
		                          IWacsConfiguration config)
		{
			this.config = config;
			this.messageHub = messageHub;
			readBallot = (Ballot) ballotGenerator.Null();
			writeBallot = (Ballot) ballotGenerator.Null();
			this.serializer = serializer;
		}

		public void SetOwner(IProcess process)
		{
			owner = process;
			listener = messageHub.Subscribe(owner);

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
			                        new Process(writeMessage.Ballot.ProcessId));
			if (writeBallot > ballot || readBallot > ballot)
			{
				messageHub.Send(new Process(message.Envelope.Sender.Process.Id),
				                new Message
					                {
						                Envelope = new Envelope {Sender = new Sender {Process = owner}},
						                Body = new Body
							                       {
								                       MessageType = FLeaseMessageType.NackWrite.ToMessageType(),
								                       Content = serializer.Serialize(new NackWritePayload {Ballot = writeMessage.Ballot})
							                       }
					                });
			}
			else
			{
				writeBallot = ballot;
				lease = new Lease(new Process(writeMessage.Lease.ProcessId),
				                  new DateTime(writeMessage.Lease.ExpiresAt, DateTimeKind.Utc));

				var msg = new AckWritePayload {Ballot = writeMessage.Ballot};
				messageHub.Send(message.Envelope.Sender.Process,
				                new Message
					                {
						                Envelope = new Envelope {Sender = new Sender {Process = owner}},
						                Body = new Body
							                       {
								                       MessageType = FLeaseMessageType.AckWrite.ToMessageType(),
								                       Content = serializer.Serialize(msg)
							                       }
					                });
			}
		}

		private void OnReadReceived(IMessage message)
		{
			var readMessage = serializer.Deserialize<ReadPayload>(message.Body.Content);
			var ballot = new Ballot(new DateTime(readMessage.Ballot.Timestamp, DateTimeKind.Utc),
			                        readMessage.Ballot.MessageNumber,
			                        new Process(readMessage.Ballot.ProcessId));
			if (writeBallot >= ballot || readBallot >= ballot)
			{
				Console.WriteLine("{0}:{1} >= {2}:{3}",
				                  writeBallot.Timestamp.ToString("hh:mm:ss fff"),
				                  writeBallot.Process.Id,
				                  ballot.Timestamp.ToString("hh:mm:ss fff"),
				                  ballot.Process.Id);
				Console.WriteLine("{0}:{1} >= {2}:{3}",
				                  readBallot.Timestamp.ToString("hh:mm:ss fff"),
				                  readBallot.Process.Id,
				                  ballot.Timestamp.ToString("hh:mm:ss fff"),
				                  ballot.Process.Id);

				messageHub.Send(new Process(message.Envelope.Sender.Process.Id),
				                new Message
					                {
						                Envelope = new Envelope {Sender = new Sender {Process = owner}},
						                Body = new Body
							                       {
								                       MessageType = FLeaseMessageType.NackRead.ToMessageType(),
								                       Content = serializer.Serialize(new NackReadPayload {Ballot = readMessage.Ballot})
							                       }
					                });
			}
			else
			{
				readBallot = ballot;
				var msg = new AckReadPayload
					          {
						          Ballot = readMessage.Ballot,
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
					          };
				messageHub.Send(message.Envelope.Sender.Process,
				                new Message
					                {
						                Envelope = new Envelope {Sender = new Sender {Process = owner}},
						                Body = new Body
							                       {
								                       MessageType = FLeaseMessageType.AckRead.ToMessageType(),
								                       Content = serializer.Serialize(msg)
							                       }
					                });
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

					var index = WaitHandle.WaitAny(new[] {ackReadFilter.Filtered, nackReadFilter.Filtered});

					if (ReadNotAcknowledged(index))
					{
						return new LeaseTxResult {TxOutcome = TxOutcome.Abort};
					}

					var lease = ackReadFilter
						.MessageStream
						.Select(m => serializer.Deserialize<AckReadPayload>(m.Body.Content))
						.Max(m => m).Lease;

					return new LeaseTxResult
						       {
							       TxOutcome = TxOutcome.Commit,
							       Lease = (lease != null)
								               ? new Lease(new Process(lease.ProcessId),
								                           new DateTime(lease.ExpiresAt, DateTimeKind.Utc))
								               : null
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

					var index = WaitHandle.WaitAny(new[] {ackWriteFilter.Filtered, nackWriteFilter.Filtered});

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
			return index == 1;
		}

		private bool FilterAckMessages<TPayload>(IBallot ballot, IMessage message, FLeaseMessageType msgType)
			where TPayload : IMessagePayload
		{
			if (message.Body.MessageType.ToMessageType() == msgType)
			{
				var ackRead = serializer.Deserialize<TPayload>(message.Body.Content);

				return ackRead.Ballot.ProcessId == ballot.Process.Id && ackRead.Ballot.Timestamp == ballot.Timestamp.Ticks;
			}

			return false;
		}

		private int GetQuorum()
		{
			return (config.FarmSize + 1) / 2;
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
			var message = new Message
				              {
					              Envelope = new Envelope {Sender = new Sender {Process = owner}},
					              Body = new Body
						                     {
							                     MessageType = FLeaseMessageType.Write.ToMessageType(),
							                     Content = serializer.Serialize(new WritePayload
								                                                    {
									                                                    Ballot = new Messages.Ballot
										                                                             {
											                                                             ProcessId = ballot.Process.Id,
											                                                             Timestamp = ballot.Timestamp.Ticks
										                                                             },
									                                                    Lease = new Messages.Lease
										                                                            {
											                                                            ProcessId = lease.Owner.Id,
											                                                            ExpiresAt = lease.ExpiresAt.Ticks
										                                                            }
								                                                    })
						                     }
				              };

			return message;
		}

		private Message CreateReadMessage(IBallot ballot)
		{
			var message = new Message
				              {
					              Envelope = new Envelope {Sender = new Sender {Process = owner}},
					              Body = new Body
						                     {
							                     MessageType = FLeaseMessageType.Read.ToMessageType(),
							                     Content = serializer.Serialize(new ReadPayload
								                                                    {
									                                                    Ballot = new Messages.Ballot
										                                                             {
											                                                             ProcessId = ballot.Process.Id,
											                                                             Timestamp = ballot.Timestamp.Ticks
										                                                             }
								                                                    })
						                     }
				              };
			return message;
		}
	}
}