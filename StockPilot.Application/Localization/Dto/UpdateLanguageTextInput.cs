using System.ComponentModel.DataAnnotations;

namespace StockPilot.Localization.Dto
{
	public class UpdateLanguageTextInput
	{
		[Required]
		[StringLength(10)]
		public string LanguageName { get; set; }

		[Required]
		[StringLength(128)]
		public string SourceName { get; set; }

		[Required]
		[StringLength(256)]
		public string Key { get; set; }

		[Required(AllowEmptyStrings = true)]
		[StringLength(67108864)]
		public string Value { get; set; }
	}
}
