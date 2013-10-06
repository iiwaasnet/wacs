using System;
using System.Threading;
using System.Threading.Tasks;

namespace wacs.FLease
{
	public class LeaseProvider : ILeaseProvider
	{
		private DateTime startTime;
		private readonly IRoundBasedRegister register;
		private readonly IBallotGenerator ballotGenerator;
		private IProcess owner;
		private readonly IFleaseConfiguration config;

		public LeaseProvider(IRoundBasedRegister register,
		                     IBallotGenerator ballotGenerator,
		                     IFleaseConfiguration config)
		{
			this.config = config;
			this.ballotGenerator = ballotGenerator;
			this.register = register;
		}

		public void Start(IProcess owner)
		{
			startTime = DateTime.UtcNow;
			this.owner = owner;
			register.SetOwner(owner);
		}

		public Task<ILease> GetLease(IBallot ballot)
		{
			return Task.Factory.StartNew(() => AсquireLease(ballot));
		}

		private ILease AсquireLease(IBallot ballot)
		{
			WaitBeforeNextLeaseIssued();

			var now = DateTime.UtcNow;

			var read = register.Read(ballot);
			if (read.TxOutcome == TxOutcome.Commit)
			{
				var lease = read.Lease;
				if (LeaseIsNotSafelyExpired(lease, now))
				{
					Sleep(config.ClockDrift);

					return AсquireLease(ballotGenerator.New(owner));
				}

				if (lease == null || lease.ExpiresAt < now || lease.Owner == owner)
				{
					lease = new Lease(owner, now + config.MaxLeaseTimeSpan);
				}

				var write = register.Write(ballot, lease);
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