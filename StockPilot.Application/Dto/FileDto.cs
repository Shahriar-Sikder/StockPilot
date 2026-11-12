using System;
using System.ComponentModel.DataAnnotations;
using Abp.Timing;

namespace StockPilot.Dto
{
	public class FileDto
	{
		[Required]
		public string FileName { get; set; }

		[Required]
		public string FileType { get; set; }

		[Required]
		public string FileToken { get; set; }

		public string FileSystemName { get; set; }

		public string FileTag { get; set; }

		public int BackGroudId { get; set; }

		public DateTime? Date { get; set; }

		public FileDto()
		{
			FileToken = Guid.NewGuid().ToString("N");
		}

		public FileDto(string fileName, string fileType)
		{
			FileName = fileName;
			FileType = fileType;
			FileToken = Guid.NewGuid().ToString("N");
			Date = Clock.Now;
		}
	}
}
