using System.ComponentModel.DataAnnotations;

namespace StockPilot.Organizations.Dto
{
	public class UserToOrganizationUnitInput
	{
		[Range(1.0, 9.223372036854776E+18)]
		public long UserId { get; set; }

		[Range(1.0, 9.223372036854776E+18)]
		public long OrganizationUnitId { get; set; }
	}
}
