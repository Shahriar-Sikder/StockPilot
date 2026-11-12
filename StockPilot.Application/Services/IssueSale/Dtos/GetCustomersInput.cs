using System.ComponentModel.DataAnnotations;

namespace StockPilot.Services.IssueSale.Dtos
{
	public class GetCustomersInput
	{
		[StringLength(200)]
		public string Filter { get; set; }

		[Range(1, 500)]
		public int MaxResultCount { get; set; } = 100;
	}
}
