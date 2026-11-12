using System.Collections.Generic;

namespace StockPilot.MultiTenancy.Dto
{
	public class TenantFeaturesForEditOutput
	{
		public int TenantId { get; set; }

		public int? EditionId { get; set; }

		public string EditionDisplayName { get; set; }

		public List<TenantFeatureEditDto> Features { get; set; } = new List<TenantFeatureEditDto>();
	}
}
