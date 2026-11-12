using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Abp.IO;

namespace StockPilot.IO
{
	public static class AppFileHelper
	{
		public static IEnumerable<string> ReadLines(string path)
		{
			using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.SequentialScan))
			{
				using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
				{
					string line;
					while ((line = sr.ReadLine()) != null)
					{
						yield return line;
					}
				}
			}
		}

		public static void DeleteFilesInFolderIfExists(string folderPath, string fileNameWithoutExtension)
		{
			foreach (FileInfo item in new DirectoryInfo(folderPath).GetFiles(fileNameWithoutExtension + ".*", SearchOption.AllDirectories).ToList())
			{
				FileHelper.DeleteIfExists(item.FullName);
			}
		}
	}
}
