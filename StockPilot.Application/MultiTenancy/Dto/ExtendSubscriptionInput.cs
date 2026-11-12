using System;
using Abp.Application.Services.Dto;

namespace StockPilot.MultiTenancy.Dto
{
	public class ExtendSubscriptionInput : EntityDto
	{
		public DateTime NewEndDate { get; set; }
	}
}
