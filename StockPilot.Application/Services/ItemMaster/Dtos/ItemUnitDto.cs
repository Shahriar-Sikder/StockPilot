using System;
using Abp.AutoMapper;
using StockPilot.Inventory;

namespace StockPilot.Services.ItemMaster.Dtos
{
	[AutoMapFrom(new Type[] { typeof(ItemUnitInfo) })]
	public class ItemUnitDto
	{
		public int UnitId { get; set; }

		public string Code { get; set; }

		public string Name { get; set; }

		public decimal Factor { get; set; }

		public bool IsBaseUnit { get; set; }
	}
}
