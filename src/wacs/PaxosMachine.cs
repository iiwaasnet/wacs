using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using wacs.FLease;

namespace wacs
{
	public class PaxosMachine : IStateMachine
	{
		private readonly int id;
		private readonly ILeaseProvider leaseProvider;
		private readonly CancellationTokenSource token;

		public PaxosMachine(int id, ILeaseProvider leaseProvider)
		{
			this.id = id;
			this.leaseProvider = leaseProvider;
			token = new CancellationTokenSource();
		}

		private void ApplyCommands(CancellationToken token)
		{
			var timer = new Stopwatch();

			while (!token.IsCancellationRequested)
			{
				var ballot = new Ballot(DateTime.UtcNow, 0, new Process(id));
				//Console.WriteLine("Get Lease for Ballot: Timestamp {0}, Process {1}", ballot.Timestamp.ToString("mm:hh:ss fff"), ballot.Process.Id);
				timer.Reset();
				timer.Start();
				var lease = leaseProvider.GetLease(ballot).Result;
				timer.Stop();
				if (lease != null)
				{
					Console.WriteLine("[{4}] Requested Ballot: Timestamp {0}, Process {1} === Received Lease: Leader {2} ExpiresAt {3} [{5}]",
						ballot.Timestamp.ToString("HH:mm:ss fff"), ballot.Process.Id, 
						lease.Owner.Id, lease.ExpiresAt.ToString("HH:mm:ss fff"),
						DateTime.UtcNow.ToString("HH:mm:ss fff"),
						timer.ElapsedMilliseconds);
				}
				else
				{
					Console.WriteLine("[{2}] Requested Ballot: Timestamp {0}, Process {1} === Received Lease: NULL [{3}]", 
						ballot.Timestamp.ToString("HH:mm:ss fff"), ballot.Process.Id,
						DateTime.UtcNow.ToString("HH:mm:ss fff"),
						timer.ElapsedMilliseconds);
				}
				if (lease != null)
				{
					Thread.Sleep(TimeSpan.FromMilliseconds(50));
				}
			}
		}

		public void Start()
		{
			leaseProvider.Start(new Process(id));
			Task.Factory.StartNew(() => ApplyCommands(token.Token), token.Token);
		}

		public void Stop()
		{
			token.Cancel();
			leaseProvider.Stop();
			token.Dispose();
		}

		public int Id
		{
			get { return id; }
		}
	}
}