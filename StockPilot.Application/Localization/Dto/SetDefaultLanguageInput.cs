using System.ComponentModel.DataAnnotations;

namespace StockPilot.Localization.Dto
{
	public class SetDefaultLanguageInput
	{
		[Required]
		[StringLength(10)]
		public virtual string Name { get; set; }
	}
}
