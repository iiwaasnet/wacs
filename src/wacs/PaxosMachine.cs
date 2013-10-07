using System;
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
			while (!token.IsCancellationRequested)
			{
				var ballot = new Ballot(DateTime.UtcNow, 0, new Process(id));
				//Console.WriteLine("Get Lease for Ballot: Timestamp {0}, Process {1}", ballot.Timestamp.ToString("mm:hh:ss fff"), ballot.Process.Id);
				var lease = leaseProvider.GetLease(ballot).Result;

				if (lease != null)
				{
					Console.WriteLine("Requested Ballot: Timestamp {0}, Process {1} === Received Lease: Leader {2} ExpiresAt {3}",
						ballot.Timestamp.ToString("HH:mm:ss fff"), ballot.Process.Id, 
						lease.Owner.Id, lease.ExpiresAt.ToString("HH:mm:ss fff"));
				}
				else
				{
					Console.WriteLine("Requested Ballot: Timestamp {0}, Process {1} === Received Lease: NULL", 
						ballot.Timestamp.ToString("HH:mm:ss fff"), ballot.Process.Id);
				}

				Thread.Sleep(TimeSpan.FromMilliseconds(5000));
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