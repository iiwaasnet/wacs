using System;
using System.Diagnostics.Contracts;
using wacs.Messaging;

namespace wacs.FLease
{
	public class MessageStreamListener : IObserver<IMessage>
	{
		private readonly Action<IMessage> callback;

		public MessageStreamListener(Action<IMessage> callback)
		{
			Contract.Requires(callback != null);

			this.callback = callback;
		}

		public void OnNext(IMessage value)
		{
			callback(value);
		}

		public void OnError(Exception error)
		{
		}

		public void OnCompleted()
		{
		}
	}
}