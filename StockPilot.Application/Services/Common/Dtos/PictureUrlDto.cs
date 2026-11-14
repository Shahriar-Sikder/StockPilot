namespace StockPilot.Services.Common.Dtos
{
	/// <summary>The saved picture of an item, category or sub-category: a relative URL such as /Pictures/t3/items/{guid}.png.</summary>
	public class PictureUrlDto
	{
		public string PictureUrl { get; set; }
	}
}
