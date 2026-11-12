namespace StockPilot.Security
{
	public class PasswordComplexitySetting
	{
		public int MinLength { get; set; }

		public int MaxLength { get; set; }

		public bool UseNumbers { get; set; }

		public bool UseUpperCaseLetters { get; set; }

		public bool UseLowerCaseLetters { get; set; }

		public bool UsePunctuations { get; set; }
	}
}
