namespace wacs.Diagnostics
{
	internal class SafePerformanceCounter<TEnum> : IPerformanceCounter
		where TEnum : struct
	{
		private readonly PerformanceCountersCategory<TEnum> countersCategory;
		private readonly TEnum counter;

		public SafePerformanceCounter(PerformanceCountersCategory<TEnum> countersCategory, TEnum counter)
		{
			this.counter = counter;
			this.countersCategory = countersCategory;
		}

		public long Increment()
		{
			return countersCategory.Increment(counter);
		}

		public long IncrementBy(long value)
		{
			return countersCategory.IncrementBy(counter, value);
		}

		public long Decrement()
		{
			return countersCategory.Decrement(counter);
		}

		public void SetValue(long value)
		{
			countersCategory.SetValue(counter, value);
		}

		public long GetRawValue()
		{
			return countersCategory.GetRawValue(counter);
		}

		public bool IsEnabled
		{
			get { return countersCategory.IsEnabled(counter); }
		}
	}
}