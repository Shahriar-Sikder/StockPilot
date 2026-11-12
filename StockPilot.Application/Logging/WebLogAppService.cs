using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Abp.Application.Services;
using Abp.Authorization;
using Abp.Dependency;
using Abp.IO;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using StockPilot.Dto;
using StockPilot.IO;
using StockPilot.Logging.Dto;

namespace StockPilot.Logging
{
	[AbpAuthorize(new string[] { "Pages.Administration.Host.Maintenance" })]
	public class WebLogAppService : StockPilotAppServiceBase, IWebLogAppService, IApplicationService, ITransientDependency
	{
		private readonly IAppFolders _appFolders;

		public WebLogAppService(IAppFolders appFolders)
		{
			_appFolders = appFolders;
		}

		public GetLatestWebLogsOutput GetLatestWebLogs()
		{
			FileInfo lastLogFile = (from f in new DirectoryInfo(_appFolders.WebLogsFolder).GetFiles("*.txt", SearchOption.TopDirectoryOnly)
				orderby f.LastWriteTime descending
				select f).FirstOrDefault();
			if (lastLogFile == null)
			{
				return new GetLatestWebLogsOutput();
			}
			List<string> lines = AppFileHelper.ReadLines(lastLogFile.FullName).Reverse().Take(1000)
				.ToList();
			int logLineCount = 0;
			int lineCount = 0;
			foreach (string line in lines)
			{
				if (line.StartsWith("DEBUG") || line.StartsWith("INFO") || line.StartsWith("WARN") || line.StartsWith("ERROR") || line.StartsWith("FATAL"))
				{
					logLineCount++;
				}
				lineCount++;
				if (logLineCount == 100)
				{
					break;
				}
			}
			return new GetLatestWebLogsOutput
			{
				LatesWebLogLines = lines.Take(lineCount).Reverse().ToList()
			};
		}

		public FileDto DownloadWebLogs()
		{
			string tempLogDirectory = CopyAllLogFilesToTempDirectory();
			List<FileInfo> logFiles = new DirectoryInfo(tempLogDirectory).GetFiles("*.txt", SearchOption.TopDirectoryOnly).ToList();
			FileDto zipFileDto = new FileDto("WebSiteLogs.zip", "application/zip");
			using (FileStream outputZipFileStream = File.Create(Path.Combine(_appFolders.TempFileDownloadFolder, zipFileDto.FileToken)))
			{
				using (ZipOutputStream zipStream = new ZipOutputStream(outputZipFileStream))
				{
					zipStream.IsStreamOwner = true;
					foreach (FileInfo logFile in logFiles)
					{
						ZipEntry logZipEntry = new ZipEntry(logFile.Name)
						{
							DateTime = logFile.LastWriteTime,
							Size = logFile.Length
						};
						zipStream.PutNextEntry(logZipEntry);
						using (FileStream fs = new FileStream(logFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.SequentialScan))
						{
							StreamUtils.Copy(fs, zipStream, new byte[4096]);
						}
						zipStream.CloseEntry();
					}
				}
			}
			Directory.Delete(tempLogDirectory, recursive: true);
			return zipFileDto;
		}

		private string CopyAllLogFilesToTempDirectory()
		{
			string tempDirectoryPath = Path.Combine(_appFolders.TempFileDownloadFolder, Guid.NewGuid().ToString("N").Substring(16));
			DirectoryHelper.CreateIfNotExists(tempDirectoryPath);
			foreach (FileInfo file in GetAllLogFiles())
			{
				string destinationFilePath = Path.Combine(tempDirectoryPath, file.Name);
				File.Copy(file.FullName, destinationFilePath, overwrite: true);
			}
			return tempDirectoryPath;
		}

		private List<FileInfo> GetAllLogFiles()
		{
			return new DirectoryInfo(_appFolders.WebLogsFolder).GetFiles("*.txt", SearchOption.TopDirectoryOnly).ToList();
		}
	}
}
