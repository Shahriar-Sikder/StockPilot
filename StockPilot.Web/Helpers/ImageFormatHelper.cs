using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace StockPilot.Web.Helpers
{
	public class ImageFormatHelper
	{
		public static ImageFormat GetRawImageFormat(byte[] fileBytes)
		{
			using (MemoryStream ms = new MemoryStream(fileBytes))
			{
				return Image.FromStream(ms).RawFormat;
			}
		}
	}
}
