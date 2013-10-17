using System;
using System.Threading;
using System.Threading.Tasks;
using wacs.Diagnostics;

namespace wacs.FLease
{
	public partial class LeaseProvider : ILeaseProvider
	{
		private DateTime startTime;
		private readonly IRoundBasedRegister register;
		private readonly IBallotGenerator ballotGenerator;
		private readonly IProcess owner;
		private readonly IFleaseConfiguration config;
		private ILease latestLease;
		private readonly ILogger logger;

		public LeaseProvider(IProcess owner,
		                     IRoundBasedRegisterFactory registerFactory,
		                     IBallotGenerator ballotGenerator,
		                     IFleaseConfiguration config,
		                     ILogger logger)
		{
			this.logger = logger;
			this.owner = owner;
			this.config = config;
			this.ballotGenerator = ballotGenerator;
			register = registerFactory.Build(owner);
		}

		public void Start()
		{
			startTime = DateTime.UtcNow;
			register.Start();
		}

		public Task<ILease> GetLease()
		{
			return Task.Factory.StartNew(() => ReadLease());
		}

		private ILease ReadLease()
		{
			WaitBeforeNextLeaseIssued();

			var now = DateTime.UtcNow;

			if (LeaseNullOrExpired(latestLease, now))
			{
				latestLease = AсquireLease(ballotGenerator.New(owner), now);
			}

			return latestLease;
		}

		private ILease AсquireLease(IBallot ballot, DateTime now)
		{
			var read = register.Read(ballot);
			if (read.TxOutcome == TxOutcome.Commit)
			{
				var lease = read.Lease;
				if (LeaseIsNotSafelyExpired(lease, now))
				{
					LogStartSleep();
					Sleep(config.ClockDrift);
					LogAwake();

					// TOOD: Add recursion exit condition
					return AсquireLease(ballotGenerator.New(owner), DateTime.UtcNow);
				}

				if (LeaseNullOrExpired(lease, now) || IsLeaseOwner(lease))
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

		private bool IsLeaseOwner(ILease lease)
		{
			return lease != null && lease.Owner == owner;
		}

		private static bool LeaseNullOrExpired(ILease lease, DateTime now)
		{
			return lease == null || lease.ExpiresAt < now;
		}

		private bool LeaseIsNotSafelyExpired(ILease lease, DateTime now)
		{
			return lease != null
			       && lease.ExpiresAt < now
			       && lease.ExpiresAt + config.ClockDrift > now;
		}

		// TODO: Move to another place, i.e. start of listeners...
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
			register.Stop();
		}
	}
}