using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace wacs.Diagnostics
{
	public class PerformanceCountersCategory<TEnum> : IPerformanceCountersCategory<TEnum>
		where TEnum : struct
	{
		private readonly ILogger logger;
		private readonly ConcurrentDictionary<TEnum, PerformanceCounter> counters;

		public PerformanceCountersCategory(ILogger logger)
		{
			this.logger = logger;
			counters = new ConcurrentDictionary<TEnum, PerformanceCounter>();
			InitPerformanceCountersCollection();
		}

		private void InitPerformanceCountersCollection()
		{
			var enumType = typeof (TEnum);

			if (!enumType.IsEnum)
			{
				throw new InvalidOperationException(string.Format("Type {0} must be an enum", enumType.Name));
			}

			var category = TryGetAttribute<PerformanceCounterCategoryAttribute>(typeof (TEnum));

			if (category == null)
			{
				throw new InvalidOperationException(string.Format("Type {0} must be decorated with the {1} attribute",
				                                                  enumType.Name,
				                                                  typeof (PerformanceCounterCategoryAttribute).Name));
			}

			foreach (var enumValue in Enum.GetValues(enumType).Cast<TEnum>())
			{
				SafeAddPerformanceCounter(enumType, enumValue, category);
			}
		}

		private void SafeAddPerformanceCounter(Type enumType, TEnum enumValue, PerformanceCounterCategoryAttribute category)
		{
			var counterDefinition = TryGetAttribute<PerformanceCounterDefinitionAttribute>(enumType.GetField(enumValue.ToString()));

			if (counterDefinition == null)
			{
				throw new InvalidOperationException(string.Format("The enum values of the {0} class must be decorated with the {1} attribute",
				                                                  enumType.FullName,
				                                                  typeof (PerformanceCounterDefinitionAttribute).Name));
			}

			try
			{
				counters[enumValue] = new PerformanceCounter(category.CategoryName, counterDefinition.Name, false);
			}
			catch (Exception err)
			{
				logger.ErrorFormat("PerformanceCounter {0}.{1} will be disabled! [{2}]", enumType.Name, enumValue, err);
			}
		}

		private static T TryGetAttribute<T>(MemberInfo type) where T : class
		{
			return Attribute.GetCustomAttribute(type, typeof (T), false) as T;
		}

		public IPerformanceCounter GetCounter(TEnum counter)
		{
			return new SafePerformanceCounter<TEnum>(this, counter);
		}

		internal long Increment(TEnum counter)
		{
			return Invoke(counter, c => c.Increment());
		}

		internal long IncrementBy(TEnum counter, long value)
		{
			return Invoke(counter, c => c.IncrementBy(value));
		}

		internal long Decrement(TEnum counter)
		{
			return Invoke(counter, c => c.Decrement());
		}

		internal void SetValue(TEnum counter, long value)
		{
			Invoke(counter, c => c.RawValue = value);
		}

		internal long GetRawValue(TEnum counter)
		{
			return Invoke(counter, c => c.RawValue);
		}

		private TResult Invoke<TResult>(TEnum counter, Func<PerformanceCounter, TResult> func)
		{
			PerformanceCounter pc;
			if (counters.TryGetValue(counter, out pc))
			{
				try
				{
					return func(pc);
				}
				catch (Exception err)
				{
					counters.TryRemove(counter, out pc);
					logger.ErrorFormat("PerformanceCounter {0}.{1} will be disabled! [{2}]", typeof(TEnum).Name, pc, err);
				}
			}

			return default(TResult);
		}

		internal bool IsEnabled(TEnum counter)
		{
			return counters.ContainsKey(counter);
		}
	}
}