using System.ComponentModel.DataAnnotations;

namespace StockPilot.Organizations.Dto
{
	public class CreateOrganizationUnitInput
	{
		public long? ParentId { get; set; }

		[Required]
		[StringLength(128)]
		public string DisplayName { get; set; }
	}
}
