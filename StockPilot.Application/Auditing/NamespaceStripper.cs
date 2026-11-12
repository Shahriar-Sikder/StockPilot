using System.Collections.Generic;
using System.Linq;
using Abp.Dependency;
using Abp.Extensions;

namespace StockPilot.Auditing
{
	public class NamespaceStripper : INamespaceStripper, ITransientDependency
	{
		public string StripNameSpace(string serviceName)
		{
			if (serviceName.IsNullOrEmpty() || !serviceName.Contains("."))
			{
				return serviceName;
			}
			if (serviceName.Contains("["))
			{
				return StripGenericNamespace(serviceName);
			}
			return GetTextAfterLastDot(serviceName);
		}

		private static string GetTextAfterLastDot(string text)
		{
			int lastDotIndex = text.LastIndexOf('.');
			return text.Substring(lastDotIndex + 1);
		}

		private static string StripGenericNamespace(string serviceName)
		{
			List<string> serviceNameParts = (from s in serviceName.Split('[')
				where !s.IsNullOrEmpty()
				select s).ToList();
			string genericServiceName = "";
			int openBracketCount = 0;
			for (int i = 0; i < serviceNameParts.Count; i++)
			{
				string serviceNamePart = serviceNameParts[i];
				if (serviceNamePart.Contains("`"))
				{
					genericServiceName = genericServiceName + GetTextAfterLastDot(serviceNamePart.Substring(0, serviceNamePart.IndexOf('`'))) + "<";
					openBracketCount++;
				}
				if (serviceNamePart.Contains(","))
				{
					genericServiceName += GetTextAfterLastDot(serviceNamePart.Substring(0, serviceNamePart.IndexOf(',')));
					if (i + 1 < serviceNameParts.Count && serviceNameParts[i + 1].Contains(","))
					{
						genericServiceName += ", ";
						continue;
					}
					genericServiceName += ">";
					openBracketCount--;
				}
			}
			for (int i2 = 0; i2 < openBracketCount; i2++)
			{
				genericServiceName += ">";
			}
			return genericServiceName;
		}
	}
}
