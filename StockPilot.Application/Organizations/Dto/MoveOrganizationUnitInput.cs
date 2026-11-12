using System.ComponentModel.DataAnnotations;

namespace StockPilot.Organizations.Dto
{
	public class MoveOrganizationUnitInput
	{
		[Range(1.0, 9.223372036854776E+18)]
		public long Id { get; set; }

		public long? NewParentId { get; set; }
	}
}
