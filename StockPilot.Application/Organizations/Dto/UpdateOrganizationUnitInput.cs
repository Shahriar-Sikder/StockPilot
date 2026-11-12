using System.ComponentModel.DataAnnotations;

namespace StockPilot.Organizations.Dto
{
	public class UpdateOrganizationUnitInput
	{
		[Range(1.0, 9.223372036854776E+18)]
		public long Id { get; set; }

		[Required]
		[StringLength(128)]
		public string DisplayName { get; set; }
	}
}
