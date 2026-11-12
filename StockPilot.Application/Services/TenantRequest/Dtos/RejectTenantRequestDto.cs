using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;

namespace StockPilot.Services.TenantRequest.Dtos
{
	public class RejectTenantRequestDto : EntityDto
	{
		[Required]
		[StringLength(1000)]
		public string ReviewNote { get; set; }
	}
}
