using System;
using System.Diagnostics;

namespace wacs.Diagnostics
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
	public sealed class PerformanceCounterDefinitionAttribute : Attribute
	{
		public PerformanceCounterDefinitionAttribute(string counterName, PerformanceCounterType type)
		{
			if (string.IsNullOrWhiteSpace(counterName))
				throw new ArgumentException("The name of the counter should be a non-empty, non-null string", "counterName");

			Name = counterName;
			Type = type;
		}

		public string Name { get; private set; }
		public string Description { get; set; }
		public PerformanceCounterType Type { get; private set; }
	}
}