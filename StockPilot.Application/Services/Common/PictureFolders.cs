using System;
using System.Linq;

namespace StockPilot.Services.Common
{
	/// <summary>The sub-folders of a tenant's picture folder (PictureFileStore).</summary>
	public static class PictureFolders
	{
		public const string Items = "items";

		public const string Categories = "categories";

		public const string SubCategories = "sub-categories";

		private static readonly string[] All = { Items, Categories, SubCategories };

		public static bool IsKnown(string folder)
		{
			return All.Contains(folder, StringComparer.Ordinal);
		}
	}
}
