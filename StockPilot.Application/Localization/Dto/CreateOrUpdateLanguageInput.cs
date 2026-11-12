using System.ComponentModel.DataAnnotations;

namespace StockPilot.Localization.Dto
{
	public class CreateOrUpdateLanguageInput
	{
		[Required]
		public ApplicationLanguageEditDto Language { get; set; }
	}
}
