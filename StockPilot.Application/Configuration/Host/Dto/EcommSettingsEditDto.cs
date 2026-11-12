namespace StockPilot.Configuration.Host.Dto
{
	public class EcommSettingsEditDto
	{
		public bool WillUseServerImage { get; set; }

		public bool WillUseCloudinaryImage { get; set; }

		public string WebSiteAddress { get; set; }

		public string ImageServerUrl { get; set; }
	}
}
