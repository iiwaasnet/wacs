using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading;
using wacs.Messaging;

namespace wacs.FLease
{
	public class AwaitableMessageStreamFilter : IObserver<IMessage>, IDisposable
	{
		private readonly Func<IMessage, bool> predicate;
		private readonly int maxCount;
		private int currentCount;
		private readonly ManualResetEventSlim waitable;
		private readonly IList<IMessage> messages;

		public AwaitableMessageStreamFilter(Func<IMessage, bool> predicate, int maxCount)
		{
			Contract.Requires(predicate != null);
			Contract.Requires(maxCount > 0);

			this.predicate = predicate;
			this.maxCount = maxCount;
			currentCount = 0;
			messages = new List<IMessage>(maxCount);
			waitable = new ManualResetEventSlim(false);
		}

		public void OnNext(IMessage value)
		{
			if (predicate(value))
			{
				if (!waitable.IsSet)
				{
					messages.Add(value);
				}
				// NOTE: decide on concurrency
				if (++currentCount == maxCount && !waitable.IsSet)
				{
					waitable.Set();
				}
			}
		}

		public void OnError(Exception error)
		{
		}

		public void OnCompleted()
		{
		}

		public void Dispose()
		{
			waitable.Dispose();
		}

		public WaitHandle Filtered
		{
			get { return waitable.WaitHandle; }
		}

		public IEnumerable<IMessage> MessageStream
		{
			get { return messages; }
		}
	}
}