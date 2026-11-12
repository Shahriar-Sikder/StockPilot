using System.ComponentModel.DataAnnotations;
using Abp.Runtime.Validation;
using StockPilot.Dto;

namespace StockPilot.Organizations.Dto
{
	public class GetOrganizationUnitUsersInput : PagedAndSortedInputDto, IShouldNormalize
	{
		[Range(1.0, 9.223372036854776E+18)]
		public long Id { get; set; }

		public void Normalize()
		{
			if (string.IsNullOrEmpty(base.Sorting))
			{
				base.Sorting = "user.Name, user.Surname";
			}
			else if (base.Sorting.Contains("userName"))
			{
				base.Sorting = base.Sorting.Replace("userName", "user.userName");
			}
			else if (base.Sorting.Contains("addedTime"))
			{
				base.Sorting = base.Sorting.Replace("addedTime", "uou.creationTime");
			}
		}
	}
}
