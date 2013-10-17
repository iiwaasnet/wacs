using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace wacs.Diagnostics
{
	public class PerformanceCountersInstaller<TEnum>
		where TEnum : struct
	{
		public PerformanceCountersInstaller()
		{
			AssertIsEnum(typeof (TEnum));
		}

		public void Install()
		{
			var categoryDefinition = GetPerformanceCounterCategory();

			DeletePerformanceCountersIfExist(categoryDefinition.CategoryName);

			PerformanceCounterCategory.Create(categoryDefinition.CategoryName,
			                                  categoryDefinition.CategoryHelp,
			                                  PerformanceCounterCategoryType.MultiInstance,
			                                  new CounterCreationDataCollection(GetPerformanceCountersData().ToArray()));
		}

		public void Uninstall()
		{
			var categoryDefinition = GetPerformanceCounterCategory();

			DeletePerformanceCountersIfExist(categoryDefinition.CategoryName);
		}

		private void DeletePerformanceCountersIfExist(string category)
		{
			if (PerformanceCounterCategory.Exists(category))
			{
				PerformanceCounterCategory.Delete(category);
			}
		}

		private IEnumerable<CounterCreationData> GetPerformanceCountersData()
		{
			var enumType = typeof (TEnum);

			var counters = Enum.GetNames(enumType)
			                   .SelectMany(enumType.GetMember)
			                   .SelectMany(CustomAttributeData.GetCustomAttributes)
			                   .Where(attr => attr.Constructor.DeclaringType == typeof (PerformanceCounterDefinitionAttribute))
			                   .Select(CreatePerformanceCounterDefinitionAttribute);
			if (!counters.Any())
			{
				throw new Exception(string.Format("Type {0} members should be decorated with {1} attribute",
				                                  typeof (TEnum),
				                                  typeof (PerformanceCounterDefinitionAttribute)));
			}

			return counters.Select(CreatePerfCounterCreationData);
		}

		private CounterCreationData CreatePerfCounterCreationData(PerformanceCounterDefinitionAttribute perfCounterDefinition)
		{
			return new CounterCreationData(perfCounterDefinition.Name,
			                               perfCounterDefinition.Description ?? string.Empty,
			                               perfCounterDefinition.Type);
		}

		private PerformanceCounterDefinitionAttribute CreatePerformanceCounterDefinitionAttribute(CustomAttributeData attrData)
		{
			return InitNamedArguments(attrData, ConstructPerformanceCounterDefinition(attrData));
		}

		private static PerformanceCounterDefinitionAttribute InitNamedArguments(CustomAttributeData attrData, PerformanceCounterDefinitionAttribute data)
		{
			foreach (var namedArgument in attrData.NamedArguments)
			{
				var mi = namedArgument.MemberInfo;

				((PropertyInfo) mi).SetValue(data, namedArgument.TypedValue.Value);
			}

			return data;
		}

		private static PerformanceCounterDefinitionAttribute ConstructPerformanceCounterDefinition(CustomAttributeData attrData)
		{
			var data = new PerformanceCounterDefinitionAttribute((string) attrData.ConstructorArguments[0].Value,
			                                                     (PerformanceCounterType) attrData.ConstructorArguments[1].Value);
			return data;
		}

		private PerformanceCounterCategoryAttribute GetPerformanceCounterCategory()
		{
			var category = CustomAttributeData.GetCustomAttributes(typeof (TEnum))
			                                  .Where(attr => attr.AttributeType == typeof (PerformanceCounterCategoryAttribute));

			if (category.Any())
			{
				return new PerformanceCounterCategoryAttribute(category.First().ConstructorArguments[0].Value.ToString());
			}

			throw new Exception(string.Format("Type {0} should be decorated with {1} attribute",
			                                  typeof (TEnum),
			                                  typeof (PerformanceCounterCategoryAttribute)));
		}

		private void AssertIsEnum(Type enumType)
		{
			if (!enumType.IsEnum)
			{
				throw new Exception(string.Format("Type {0} should be an enum", enumType));
			}
		}
	}
}