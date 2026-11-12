using Abp.Application.Services.Dto;

namespace StockPilot.MultiTenancy.Dto
{
	public class SetTenantActiveInput : EntityDto
	{
		public bool IsActive { get; set; }
	}
}
