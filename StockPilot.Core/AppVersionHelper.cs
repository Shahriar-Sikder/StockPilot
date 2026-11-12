using System;
using System.IO;

namespace StockPilot
{
	public class AppVersionHelper
	{
		public const string Version = "4.3.0.0";

		public static DateTime ReleaseDate => new FileInfo(typeof(AppVersionHelper).Assembly.Location).LastWriteTime;
	}
}
