using System;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using StockPilot.Entity;

namespace StockPilot.Services.ItemMaster.Dtos
{
	[AutoMapFrom(new Type[] { typeof(StockPilot.Entity.ItemMaster) })]
	public class ItemMasterComboDto : EntityDto
	{
		public string ItemCode { get; set; }

		public string ItemName { get; set; }

		public int ItemCategoryId { get; set; }

		public int BaseUnitId { get; set; }

		public bool IsStockItem { get; set; }
	}
}
