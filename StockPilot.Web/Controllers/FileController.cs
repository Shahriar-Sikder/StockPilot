using System.IO;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using Abp.Auditing;
using Abp.UI;
using Abp.Web.Mvc.Authorization;
using StockPilot.Dto;

namespace StockPilot.Web.Controllers
{
	[AbpMvcAuthorize(new string[] { })]
	public class FileController : StockPilotControllerBase
	{
		private static readonly Regex TokenPattern = new Regex("^[0-9a-f]{32}$", RegexOptions.Compiled);

		private readonly IAppFolders _appFolders;

		public FileController(IAppFolders appFolders)
		{
			_appFolders = appFolders;
		}

		[DisableAuditing]
		public ActionResult DownloadTempFile(FileDto file)
		{
			if (file == null || string.IsNullOrEmpty(file.FileToken) || !TokenPattern.IsMatch(file.FileToken))
			{
				throw new UserFriendlyException(L("RequestedFileDoesNotExists"));
			}
			string path = Path.Combine(_appFolders.TempFileDownloadFolder, file.FileToken);
			if (!System.IO.File.Exists(path))
			{
				throw new UserFriendlyException(L("RequestedFileDoesNotExists"));
			}
			byte[] bytes = System.IO.File.ReadAllBytes(path);
			System.IO.File.Delete(path);
			string fileName = Path.GetFileName(file.FileName ?? "download");
			return File(bytes, string.IsNullOrEmpty(file.FileType) ? "application/octet-stream" : file.FileType, fileName);
		}
	}
}
