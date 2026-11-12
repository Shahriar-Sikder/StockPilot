using System.ComponentModel.DataAnnotations;

namespace StockPilot.Services.Common.Dtos
{
	public class PictureContentDto
	{
		[StringLength(255)]
		public string FileName { get; set; }

		[Required]
		public string Base64Content { get; set; }
	}
}
