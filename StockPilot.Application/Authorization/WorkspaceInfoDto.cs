namespace StockPilot.Authorization
{
	public class WorkspaceInfoDto
	{
		public int? TenantId { get; set; }

		public long? UserId { get; set; }

		public string[] Permissions { get; set; }
	}
}
