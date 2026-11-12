using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace StockPilot.Common.Extensions
{
	public static class EnumExtensions
	{
		public static string GetEnamDisplayName(this Enum enumValue)
		{
			return enumValue.GetType().GetMember(enumValue.ToString()).First()
				.GetCustomAttribute<DisplayAttribute>()
				.GetName();
		}
	}
}
