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
		private readonly Ballot writeBallot;
		private ILease lease;
		private IListener listener;

		private IObservable<IMessage> ackReadStream;
		private IObservable<IMessage> nackReadStream;
		private IObservable<IMessage> ackWriteStream;
		private IObservable<IMessage> nackWriteStream;
		private readonly IMessageSerializer serializer;

		public RoundBasedRegister(IMessageHub messageHub, IBallotGenerator ballotGenerator, IMessageSerializer serializer)
		{
			this.messageHub = messageHub;
			readBallot = (Ballot) ballotGenerator.Null();
			writeBallot = (Ballot) ballotGenerator.Null();
			this.serializer = serializer;
		}

		public void SetOwner(IProcess process)
		{
			this.owner = owner;
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
			throw new NotImplementedException();
		}

		private void OnReadReceived(IMessage message)
		{
			var readMsg = serializer.Deserialize<ReadMessage>(message.Body.Content);
			var ballot = new Ballot(readMsg.Ballot.Timestamp, readMsg.Ballot.MessageNumber, new Process(readMsg.Ballot.ProcessId));
			if (writeBallot >= ballot || readBallot >= ballot)
			{
				messageHub.Send(new Process(message.Envelope.Sender.Process.Id),
				                new Message
					                {
						                Envelope = new Envelope {Sender = new Sender {Process = owner}},
						                Body = new Body
							                       {
								                       MessageType = FLeaseMessageType.NackRead.ToMessageType(),
								                       Content = serializer.Serialize(new NackReadMessage {Ballot = readMsg.Ballot})
							                       }
					                });
			}
			else
			{
				readBallot = ballot;
				var msg = new AckReadMessage
					          {
						          Ballot = readMsg.Ballot,
						          KnownWriteBallot = new Messages.Ballot
							                             {
								                             ProcessId = writeBallot.Process.Id,
								                             Timestamp = writeBallot.Timestamp,
								                             MessageNumber = writeBallot.MessageNumber
							                             },
						          Lease = new Messages.Lease
							                  {
								                  ProcessId = lease.Owner.Id,
								                  ExpiresAt = lease.ExpiresAt
							                  }
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
			var ackReadFilter = new AwaitableMessageStreamFilter(m => FilterAckReadMessages(ballot, m), GetQuorum());
			var nackReadFilter = new AwaitableMessageStreamFilter(m => FilterNackReadMessages(ballot, m), GetQuorum());
			ackReadStream.Subscribe(ackReadFilter);
			nackReadStream.Subscribe(nackReadFilter);

			var message = CreateReadMessage(ballot);
			messageHub.Broadcast(message);

			var index = WaitHandle.WaitAny(new[] {ackReadFilter.Filtered, nackReadFilter.Filtered});

			if (ReadNotAcknowledged(index))
			{
				return new LeaseTxResult {TxOutcome = TxOutcome.Abort};
			}

			var lease = ackReadFilter
				.MessageStream
				.Select(m => serializer.Deserialize<AckReadMessage>(m.Body.Content))
				.Max(m => m).Lease;

			return new LeaseTxResult
				       {
					       TxOutcome = TxOutcome.Commit,
					       Lease = new Lease(new Process(lease.ProcessId), lease.ExpiresAt)
				       };
		}

		private bool ReadNotAcknowledged(int index)
		{
			return index == 1;
		}

		private bool FilterNackReadMessages(IBallot ballot, IMessage message)
		{
			if (message.Body.MessageType.ToMessageType() == FLeaseMessageType.NackRead)
			{
				var ackRead = serializer.Deserialize<NackReadMessage>(message.Body.Content);

				return ackRead.Ballot.ProcessId == ballot.Process.Id && ackRead.Ballot.Timestamp == ballot.Timestamp;
			}

			return false;
		}

		private bool FilterAckReadMessages(IBallot ballot, IMessage message)
		{
			if (message.Body.MessageType.ToMessageType() == FLeaseMessageType.AckRead)
			{
				var ackRead = serializer.Deserialize<AckReadMessage>(message.Body.Content);

				return ackRead.Ballot.ProcessId == ballot.Process.Id && ackRead.Ballot.Timestamp == ballot.Timestamp;
			}

			return false;
		}

		private int GetQuorum()
		{
			throw new NotImplementedException();
		}

		public ILeaseTxResult Write(IBallot ballot, ILease lease)
		{
			throw new System.NotImplementedException();
		}

		public void Start()
		{
			listener.Start();
		}

		public void Stop()
		{
			listener.Stop();
		}

		private Message CreateReadMessage(IBallot ballot)
		{
			var message = new Message
				              {
					              Envelope = new Envelope {Sender = new Sender {Process = owner}},
					              Body = new Body
						                     {
							                     MessageType = FLeaseMessageType.Read.ToMessageType(),
							                     Content = serializer.Serialize(new ReadMessage
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