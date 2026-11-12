using System.Text.RegularExpressions;
using Abp.Extensions;

namespace StockPilot.Security
{
	public class PasswordComplexityChecker
	{
		public bool Check(PasswordComplexitySetting setting, string password)
		{
			if (password.IsNullOrEmpty())
			{
				return false;
			}
			if (password.Length < setting.MinLength)
			{
				return false;
			}
			if (password.Length > setting.MaxLength)
			{
				return false;
			}
			if (setting.UseUpperCaseLetters && !new Regex("[A-Z]").IsMatch(password))
			{
				return false;
			}
			if (setting.UseLowerCaseLetters && !new Regex("[a-z]").IsMatch(password))
			{
				return false;
			}
			if (setting.UseNumbers && !new Regex("[0-9]").IsMatch(password))
			{
				return false;
			}
			if (setting.UsePunctuations && !new Regex("[\\p{P}\\p{S}]").IsMatch(password))
			{
				return false;
			}
			return true;
		}
	}
}
