namespace wacs.Diagnostics
{
	public interface IPerformanceCountersCategory<in TEnum> 
		where TEnum : struct
	{
		IPerformanceCounter GetCounter(TEnum counter);
	}
}