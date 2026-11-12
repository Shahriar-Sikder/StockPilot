using Abp.Runtime.Validation;
using StockPilot.Dto;

namespace StockPilot.MultiTenancy.Dto
{
	public class GetTenantsInput : PagedAndSortedInputDto, IShouldNormalize
	{
		public string Filter { get; set; }

		public bool? IsActive { get; set; }

		public int? EditionId { get; set; }

		public TenantSubscriptionStatus? SubscriptionStatus { get; set; }

		public bool? HasCustomFeatures { get; set; }

		public void Normalize()
		{
			if (string.IsNullOrEmpty(base.Sorting))
			{
				base.Sorting = "TenancyName";
			}
			base.Sorting = base.Sorting.Replace("editionDisplayName", "Edition.DisplayName");
		}
	}
}
