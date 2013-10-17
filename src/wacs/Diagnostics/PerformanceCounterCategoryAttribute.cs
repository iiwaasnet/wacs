using System;

namespace wacs.Diagnostics
{
	[AttributeUsage(AttributeTargets.Enum, AllowMultiple = false, Inherited = false)]
	public sealed class PerformanceCounterCategoryAttribute : Attribute
	{
		public PerformanceCounterCategoryAttribute(string categoryName)
		{
			if (string.IsNullOrWhiteSpace(categoryName))
			{
				throw new ArgumentException("The name of the category must be a non-empty string", "categoryName");
			}

			CategoryName = categoryName;
		}

		public string CategoryName { get; private set; }

		public string CategoryHelp { get; set; }
	}
}