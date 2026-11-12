using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StockPilot.Services.PurchaseInvoice
{
	public static class PurchaseDocumentRules
	{
		public const int MaxBytes = 3145728;

		private static readonly Dictionary<string, string> ContentTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{ ".pdf", "application/pdf" },
			{ ".png", "image/png" },
			{ ".jpg", "image/jpeg" },
			{ ".jpeg", "image/jpeg" },
			{ ".webp", "image/webp" },
			{ ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
			{ ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
			{ ".csv", "text/csv" },
			{ ".txt", "text/plain" }
		};

		public static IEnumerable<string> AllowedExtensions => ContentTypes.Keys;

		public static string CleanFileName(string fileName)
		{
			string name = Path.GetFileName((fileName ?? string.Empty).Replace('\\', '/').Split('/').Last()).Trim();
			if (name.Length <= 255)
			{
				return name;
			}
			return name.Substring(name.Length - 255);
		}

		public static string GetContentTypeOrNull(string fileName)
		{
			if (!ContentTypes.TryGetValue(Path.GetExtension(fileName ?? string.Empty), out var contentType))
			{
				return null;
			}
			return contentType;
		}

		public static bool ContentMatchesExtension(string fileName, byte[] bytes)
		{
			string text = Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();
			if (text != null)
			{
				int length = text.Length;
				if (length != 4)
				{
					if (length == 5)
					{
						char c = text[1];
						if ((uint)c <= 106u)
						{
							if (c != 'd')
							{
								if (c == 'j' && text == ".jpeg")
								{
									goto IL_0183;
								}
							}
							else if (text == ".docx")
							{
								goto IL_01dd;
							}
						}
						else if (c != 'w')
						{
							if (c == 'x' && text == ".xlsx")
							{
								goto IL_01dd;
							}
						}
						else if (text == ".webp")
						{
							if (StartsWith(bytes, 82, 73, 70, 70) && bytes.Length >= 12 && bytes[8] == 87 && bytes[9] == 69 && bytes[10] == 66)
							{
								return bytes[11] == 80;
							}
							return false;
						}
					}
				}
				else
				{
					char c = text[2];
					if ((uint)c <= 110u)
					{
						switch (c)
						{
						case 'd':
							if (!(text == ".pdf"))
							{
								break;
							}
							return StartsWith(bytes, 37, 80, 68, 70);
						case 'n':
							if (!(text == ".png"))
							{
								break;
							}
							return StartsWith(bytes, 137, 80, 78, 71);
						}
					}
					else if (c != 'p')
					{
						if (c != 's')
						{
							if (c == 'x' && text == ".txt")
							{
								goto IL_01f5;
							}
						}
						else if (text == ".csv")
						{
							goto IL_01f5;
						}
					}
					else if (text == ".jpg")
					{
						goto IL_0183;
					}
				}
			}
			return false;
			IL_01dd:
			return StartsWith(bytes, 80, 75, 3, 4);
			IL_01f5:
			return !bytes.Take(8000).Contains<byte>(0);
			IL_0183:
			return StartsWith(bytes, 255, 216, 255);
		}

		private static bool StartsWith(byte[] bytes, params byte[] signature)
		{
			if (bytes.Length < signature.Length)
			{
				return false;
			}
			for (int i = 0; i < signature.Length; i++)
			{
				if (bytes[i] != signature[i])
				{
					return false;
				}
			}
			return true;
		}
	}
}
