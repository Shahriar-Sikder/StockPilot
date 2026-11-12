namespace StockPilot.MultiTenancy.Dto
{
	public class TenantFeatureEditDto
	{
		public string Name { get; set; }

		public string DisplayName { get; set; }

		public string Description { get; set; }

		public string ParentName { get; set; }

		public int Depth { get; set; }

		public string InputType { get; set; }

		public int? MinValue { get; set; }

		public int? MaxValue { get; set; }

		public string EditionValue { get; set; }

		public string TenantValue { get; set; }

		public string EffectiveValue { get; set; }

		public bool IsOverridden { get; set; }
	}
}
