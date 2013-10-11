using System;
using System.Diagnostics;
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
		private volatile ILease latestLease;
		private readonly AutoResetEvent renewalGateway;

		public LeaseProvider(IRoundBasedRegister register,
		                     IBallotGenerator ballotGenerator,
		                     IFleaseConfiguration config)
		{
			this.config = config;
			this.ballotGenerator = ballotGenerator;
			this.register = register;
			renewalGateway = new AutoResetEvent(false);
			//new Thread(ScheduleLeaseRenewal).Start();
		}

		public void Start(IProcess owner)
		{
			startTime = DateTime.UtcNow;
			this.owner = owner;
			register.SetOwner(owner);
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

				//if (IsLeaseOwner(latestLease))
				//{
				//	renewalGateway.Set();
				//}
			}

			//if (LeaseIsNotSafelyExpired(latestLease, now))
			//{
			//	Console.WriteLine("SLEEP === Process {0} Sleep from {1}", owner.Id, DateTime.UtcNow.ToString("HH:mm:ss fff"));
			//	Sleep(config.ClockDrift);
			//	Console.WriteLine("SLEEP === Process {0} Waked up at {1}", owner.Id, DateTime.UtcNow.ToString("HH:mm:ss fff"));

			//	latestLease = AсquireLease(ballotGenerator.New(owner), now);
			//}

			return latestLease;
		}

		private void ScheduleLeaseRenewal()
		{
			renewalGateway.WaitOne();

			var lease = latestLease;
			var awaitable = new ManualResetEventSlim(false);

			if (lease != null)
			{
				awaitable.Wait(lease.ExpiresAt - DateTime.UtcNow - TimeSpan.FromSeconds(2));

				var timer = new Stopwatch();
				timer.Start();
				Console.WriteLine("[{0}] Process {1} === RENEW STARTED", DateTime.UtcNow.ToString("HH:mm:ss fff"), owner.Id);

				var renewedLease = AсquireLease(ballotGenerator.New(owner), DateTime.UtcNow);

				timer.Stop();
				if (renewedLease != null)
				{
					latestLease = lease;

					
					Console.WriteLine("[{4}] Process {0} === RENEWED LEASE: Leader {1} ExpiresAt {2} [{3}]",
					                  owner.Id,
					                  lease.Owner.Id,
					                  lease.ExpiresAt.ToString("HH:mm:ss fff"),
					                  timer.ElapsedMilliseconds,
					                  DateTime.UtcNow.ToString("HH:mm:ss fff"));
				}
				else
				{
					Console.WriteLine("[{2}] Process {0} === RENEWED LEASE: NULL [{1}]",
									  owner.Id,
									  timer.ElapsedMilliseconds,
									  DateTime.UtcNow.ToString("HH:mm:ss fff"));
				}
			}
		}

		private ILease AсquireLease(IBallot ballot, DateTime now)
		{
			var read = register.Read(ballot);
			if (read.TxOutcome == TxOutcome.Commit)
			{
				var lease = read.Lease;
				if (LeaseIsNotSafelyExpired(lease, now))
				{
					Console.WriteLine("SLEEP === Process {0} Sleep from {1}", owner.Id, DateTime.UtcNow.ToString("HH:mm:ss fff"));
					Sleep(config.ClockDrift);
					Console.WriteLine("SLEEP === Process {0} Waked up at {1}", owner.Id, DateTime.UtcNow.ToString("HH:mm:ss fff"));

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