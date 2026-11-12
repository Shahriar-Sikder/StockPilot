using Abp.Runtime.Validation;
using StockPilot.Dto;

namespace StockPilot.Authorization.Users.Dto
{
	public class GetUsersInput : PagedAndSortedInputDto, IShouldNormalize
	{
		public string Filter { get; set; }

		public string Permission { get; set; }

		public int? Role { get; set; }

		public void Normalize()
		{
			if (string.IsNullOrEmpty(base.Sorting))
			{
				base.Sorting = "Name,Surname";
			}
		}
	}
}
