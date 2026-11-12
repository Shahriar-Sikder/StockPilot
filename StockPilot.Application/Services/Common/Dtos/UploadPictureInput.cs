using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;

namespace StockPilot.Services.Common.Dtos
{
	public class UploadPictureInput : EntityDto
	{
		[Required]
		public string FileName { get; set; }

		[Required]
		public string Base64Content { get; set; }
	}
}
