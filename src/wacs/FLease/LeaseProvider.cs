using System;
using System.Threading;
using System.Threading.Tasks;
using wacs.Configuration;
using wacs.Diagnostics;
using wacs.Resolver;

namespace wacs.FLease
{
    public partial class LeaseProvider : ILeaseProvider
    {
        private readonly IBallotGenerator ballotGenerator;
        private readonly ILeaseConfiguration config;
        private readonly Timer leaseTimer;
        private readonly ILogger logger;
        private readonly IProcess owner;
        private readonly IRoundBasedRegister register;
        private readonly SemaphoreSlim renewGateway;
        private volatile ILease lastKnownLease;

        public LeaseProvider(IRoundBasedRegister register,
                             IBallotGenerator ballotGenerator,
                             ILeaseConfiguration config,
                             INodeResolver nodeResolver,
                             ILogger logger)
        {
            ValidateConfiguration(config);

            WaitBeforeNextLeaseIssued(config);

            owner = nodeResolver.ResolveLocalNode();
            this.logger = logger;
            this.config = config;
            this.ballotGenerator = ballotGenerator;
            this.register = register;

            renewGateway = new SemaphoreSlim(1);
            leaseTimer = new Timer(state => ScheduledReadOrRenewLease(), null, TimeSpan.FromMilliseconds(-1), TimeSpan.FromMilliseconds(-1));
        }

        public void ResetLease()
        {
            Interlocked.Exchange(ref lastKnownLease, null);
        }

        public Task<ILease> GetLease()
        {
            return Task.Factory.StartNew(() => GetLastKnownLease());
        }

        public void Dispose()
        {
            leaseTimer.Change(TimeSpan.FromMilliseconds(-1), TimeSpan.FromMilliseconds(-1));
            leaseTimer.Dispose();
            register.Dispose();
            renewGateway.Dispose();
        }

        private void ValidateConfiguration(ILeaseConfiguration config)
        {
            if (config.NodeResponseTimeout.TotalMilliseconds * 2 > config.MessageRoundtrip.TotalMilliseconds)
            {
                throw new Exception(string.Format("NodeResponseTimeout[{0} msec] should be at least 2 times shorter than MessageRoundtrip[{1} msec]!",
                                                  config.NodeResponseTimeout.TotalMilliseconds,
                                                  config.MessageRoundtrip.TotalMilliseconds));
            }
            if (config.MaxLeaseTimeSpan
                - TimeSpan.FromTicks(config.MessageRoundtrip.Ticks * 2)
                - config.ClockDrift <= TimeSpan.FromMilliseconds(0))
            {
                throw new Exception(string.Format("MaxLeaseTimeSpan[{0} msec] should be longer than (2 * MessageRoundtrip[{1} msec] + ClockDrift[{2} msec])",
                                                  config.MaxLeaseTimeSpan.TotalMilliseconds,
                                                  config.MessageRoundtrip.TotalMilliseconds,
                                                  config.ClockDrift.TotalMilliseconds));
            }
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
                       ? config.MaxLeaseTimeSpan
                         - TimeSpan.FromTicks(config.MessageRoundtrip.Ticks * 2)
                         - config.ClockDrift
                       : TimeSpan.FromTicks(config.MaxLeaseTimeSpan.Ticks);
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

                logger.InfoFormat("Write lease: Owner {0}", lease.Owner);
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

        private void WaitBeforeNextLeaseIssued(ILeaseConfiguration config)
        {
            Sleep(config.MaxLeaseTimeSpan);
        }

        private void Sleep(TimeSpan delay)
        {
            using (var @lock = new ManualResetEvent(false))
            {
                @lock.WaitOne(delay);
            }
        }

        //TODO: add Dispose() method???
    }
}