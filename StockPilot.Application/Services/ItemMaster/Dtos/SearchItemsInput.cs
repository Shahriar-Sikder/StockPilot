using System.ComponentModel.DataAnnotations;

namespace StockPilot.Services.ItemMaster.Dtos
{
	public class SearchItemsInput
	{
		[StringLength(100)]
		public string Text { get; set; }

		[Range(1, 20)]
		public int MaxResultCount { get; set; } = 6;
	}
}
