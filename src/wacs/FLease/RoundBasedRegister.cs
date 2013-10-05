using System;
using System.Reactive.Linq;
using wacs.FLease.Messages;
using wacs.Messaging;

namespace wacs.FLease
{
	public class RoundBasedRegister : IRoundBasedRegister
	{
		private IProcess owner;
		private readonly IMessageHub messageHub;
		private IBallot readBallot;
		private IBallot writeBallot;
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
			readBallot = ballotGenerator.Null();
			writeBallot = ballotGenerator.Null();
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
			throw new NotImplementedException();
		}

		public ILeaseTxResult Read(IBallot ballot)
		{
			var ackReadFilter = new AwaitableMessageStreamFilter(m => FilterAckReadMessages(ballot, m), GetQuorum());
			var message = CreateReadMessage(ballot);

			messageHub.Broadcast(message);
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

		private bool FilterAckReadMessages(IBallot ballot, IMessage message)
		{
			if (message.Body.MessageType.ToMessageType() == FLeaseMessageType.AckRead)
			{
				var ackRead = serializer.Deserialize<AckReadMessage>(message.Body.Content);

				return ackRead.Ballot.ProcessId == ballot.Process.Id && ackRead.Ballot.Timestamp == ballot.Timestamp.Ticks;
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
	}
}