using System.Text.RegularExpressions;
using Abp.Extensions;

namespace StockPilot.Validation
{
	public class ValidationHelper
	{
		public const string EmailRegex = "^\\w+([-+.']\\w+)*@\\w+([-.]\\w+)*\\.\\w+([-.]\\w+)*$";

		public bool IsEmail(string value)
		{
			if (value.IsNullOrEmpty())
			{
				return false;
			}
			return new Regex("^\\w+([-+.']\\w+)*@\\w+([-.]\\w+)*\\.\\w+([-.]\\w+)*$").IsMatch(value);
		}
	}
}
