using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace StockPilot.Services.Common
{
	public static class PictureHelper
	{
		public const int MaxUploadBytes = 3145728;

		public const int MaxDimension = 220;

		public const string ContentType = "image/png";

		public static byte[] DecodeBase64(string base64Content)
		{
			if (string.IsNullOrWhiteSpace(base64Content))
			{
				return null;
			}
			int comma = base64Content.IndexOf(',');
			if (base64Content.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && comma > 0)
			{
				base64Content = base64Content.Substring(comma + 1);
			}
			try
			{
				return Convert.FromBase64String(base64Content.Trim());
			}
			catch (FormatException)
			{
				return null;
			}
		}

		public static byte[] ResizeToPng(byte[] bytes, int maxDimension = 220)
		{
			try
			{
				using (MemoryStream input = new MemoryStream(bytes))
				{
					using (Image source = Image.FromStream(input))
					{
						double scale = Math.Min(1.0, (double)maxDimension / (double)Math.Max(source.Width, source.Height));
						int width = Math.Max(1, (int)Math.Round((double)source.Width * scale));
						int height = Math.Max(1, (int)Math.Round((double)source.Height * scale));
						using (Bitmap target = new Bitmap(width, height, PixelFormat.Format32bppArgb))
						{
							using (Graphics graphics = Graphics.FromImage(target))
							{
								graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
								graphics.SmoothingMode = SmoothingMode.HighQuality;
								graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
								graphics.DrawImage(source, 0, 0, width, height);
							}
							using (MemoryStream output = new MemoryStream())
							{
								target.Save(output, ImageFormat.Png);
								return output.ToArray();
							}
						}
					}
				}
			}
			catch (ArgumentException)
			{
				return null;
			}
			catch (OutOfMemoryException)
			{
				return null;
			}
		}
	}
}
