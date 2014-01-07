namespace wacs.Diagnostics
{
	public interface IPerformanceCounter
	{
		long Increment();

		long IncrementBy(long value);

		long Decrement();

		void SetValue(long value);

		long GetRawValue();

		bool IsEnabled { get; } 
	}
}