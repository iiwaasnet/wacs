using System;
using System.Threading;
using System.Threading.Tasks;

namespace wacs.FLease
{
	public class LeaseProvider : ILeaseProvider
	{
		private readonly IFleaseConfiguration config;
		private DateTime startTime;
		private readonly ILeaseReader leaseReader;
		private readonly ILeaseWriter leaseWriter;
		private readonly IBallotGenerator ballotGenerator;
		private IProcess owner;

		public LeaseProvider(ILeaseReader leaseReader,
		                     ILeaseWriter leaseWriter,
		                     IBallotGenerator ballotGenerator,
		                     IFleaseConfiguration config)
		{
			this.config = config;
			this.ballotGenerator = ballotGenerator;
			this.leaseReader = leaseReader;
			this.leaseWriter = leaseWriter;
		}

		public void Start(IProcess owner)
		{
			startTime = DateTime.UtcNow;
			this.owner = owner;
		}

		public Task<ILease> GetLease(IBallot ballot)
		{
			return Task.Factory.StartNew(() => AсquireLease(ballot));
		}

		private ILease AсquireLease(IBallot ballot)
		{
			WaitBeforeNextLeaseIssued();

			var now = DateTime.UtcNow;

			var read = leaseReader.Read(ballot);
			if (read.TxOutcome == TxOutcome.Commit)
			{
				var lease = read.Lease;
				if (LeaseIsNotSafelyExpired(lease, now))
				{
					Sleep(config.ClockDrift);

					return AсquireLease(ballotGenerator.Create());
				}

				if (lease == null || lease.ExpiresAt < now || lease.Owner == owner)
				{
					lease = new Lease(owner, now + config.MaxLeaseTimeSpan);
				}

				var write = leaseWriter.Write(ballot, lease);
				if (write.TxOutcome == TxOutcome.Commit)
				{
					return lease;
				}
			}

			return null;
		}

		private bool LeaseIsNotSafelyExpired(ILease lease, DateTime now)
		{
			return lease != null
			       && lease.ExpiresAt < now
			       && lease.ExpiresAt + config.ClockDrift > now;
		}

		private void WaitBeforeNextLeaseIssued()
		{
			var diff = DateTime.UtcNow - startTime;

			if (diff < config.MaxLeaseTimeSpan)
			{
				Sleep(config.MaxLeaseTimeSpan - diff);
			}
		}

		private void Sleep(TimeSpan delay)
		{
			using (var @lock = new ManualResetEvent(false))
			{
				@lock.WaitOne(delay);
			}
		}

		public void Stop()
		{
		}
	}
}