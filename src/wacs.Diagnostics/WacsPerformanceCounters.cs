﻿using System.Diagnostics;

namespace wacs.Diagnostics
{
	[PerformanceCounterCategory("wacs")]
	public enum WacsPerformanceCounters
	{
        [PerformanceCounterDefinition("Consensus Agreements/sec", PerformanceCounterType.RateOfCountsPerSecond64)]
        ConsensusAgreementsPerSecond
	}
}