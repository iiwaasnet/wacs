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
        private volatile ILease lastKnownLease;
        private readonly ILogger logger;
        private readonly Timer leaseTimer;
        private readonly SemaphoreSlim renewGateway;

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

            renewGateway = new SemaphoreSlim(1);
            leaseTimer = new Timer(state => ScheduledReadOrRenewLease(), null, TimeSpan.FromMilliseconds(-1), TimeSpan.FromMilliseconds(-1));
        }

        private void ScheduledReadOrRenewLease()
        {
            if (renewGateway.Wait(TimeSpan.FromMilliseconds(10)))
            {
                try
                {
                    ReadOrRenewLease();
                }
                catch (Exception err)
                {
                    logger.Error(err);
                }
                finally
                {
                    renewGateway.Release();
                }
            }
        }

        private void ReadOrRenewLease()
        {
            WaitBeforeNextLeaseIssued();

            var now = DateTime.UtcNow;
            var lease = AсquireOrLearnLease(ballotGenerator.New(owner), now);

            if (ProcessBecameLeader(lease, lastKnownLease) || ProcessLostLeadership(lease, lastKnownLease))
            {
                var renewPeriod = CalcLeaseRenewPeriod(ProcessBecameLeader(lease, lastKnownLease));
                leaseTimer.Change(renewPeriod, renewPeriod);
            }

            lastKnownLease = lease;
        }

        private bool ProcessLostLeadership(ILease nextLease, ILease previousLease)
        {
            return (previousLease != null && previousLease.Owner.Equals(owner)
                    && nextLease != null && !nextLease.Owner.Equals(owner));
        }

        private bool ProcessBecameLeader(ILease nextLease, ILease previousLease)
        {
            return ((previousLease == null || !previousLease.Owner.Equals(owner))
                    && nextLease != null && nextLease.Owner.Equals(owner));
        }

        private TimeSpan CalcLeaseRenewPeriod(bool leader)
        {
            return (leader)
                       ? TimeSpan.FromTicks(config.MaxLeaseTimeSpan.Ticks / 2 - config.ClockDrift.Ticks)
                       : TimeSpan.FromTicks(config.MaxLeaseTimeSpan.Ticks);
        }

        public void Start()
        {
            startTime = DateTime.UtcNow;
            register.Start();
            leaseTimer.Change(TimeSpan.FromMilliseconds(0), config.MaxLeaseTimeSpan);
        }

        public Task<ILease> GetLease()
        {
            return Task.Factory.StartNew(() => GetLastKnownLease());
        }

        private ILease GetLastKnownLease()
        {
            var now = DateTime.UtcNow;

            renewGateway.Wait();
            try
            {
                if (LeaseNullOrExpired(lastKnownLease, now))
                {
                    ReadOrRenewLease();
                }

                return lastKnownLease;
            }
            finally
            {
                renewGateway.Release();
            }
        }

        private ILease AсquireOrLearnLease(IBallot ballot, DateTime now)
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
                    return AсquireOrLearnLease(ballotGenerator.New(owner), DateTime.UtcNow);
                }

                if (LeaseNullOrExpired(lease, now) || IsLeaseOwner(lease))
                {
                    LogLeaseProlonged(lease);
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
            return lease != null && lease.Owner.Equals(owner);
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

        //TODO: add Dispose() method???
        public void Stop()
        {
            register.Stop();
            leaseTimer.Change(TimeSpan.FromMilliseconds(-1), TimeSpan.FromMilliseconds(-1));
        }

        public void Dispose()
        {
            leaseTimer.Dispose();
            renewGateway.Dispose();
        }
    }
}