using System.Threading.Tasks;

namespace StockPilot.Services.Common
{
	/// <summary>
	/// Item, category and sub-category pictures as PNG files on disk. Only the relative URL
	/// (e.g. /Pictures/t3/items/9f1c….png) is saved with the record. Everything else that uses
	/// AppBinaryObjects (invoice documents, logos, profile pictures) stays in the database.
	/// </summary>
	public interface IPictureFileStore
	{
		/// <summary>
		/// Decodes, checks (3 MB) and resizes the picture like PictureStore, writes it to
		/// {root}\{t{tenantId} | host}\{folder}\{new guid}.png and returns its relative URL.
		/// </summary>
		/// <param name="tenantId">The owner; null is the host.</param>
		/// <param name="folder">One of <see cref="PictureFolders"/>.</param>
		/// <param name="base64Content">The picture as Base64, with or without a data: prefix.</param>
		Task<string> SaveAsync(int? tenantId, string folder, string base64Content);

		/// <summary>
		/// Saves the new picture, then deletes the previous file. If the new picture is refused, the
		/// previous file is left as it is. Returns the new URL.
		/// </summary>
		Task<string> ReplaceAsync(int? tenantId, string folder, string previousPictureUrl, string base64Content);

		/// <summary>
		/// Deletes the file behind a URL returned by <see cref="SaveAsync"/>. An empty URL or a missing file is
		/// ignored. A URL that is not under the picture root is refused.
		/// </summary>
		Task DeleteAsync(string pictureUrl);

		/// <summary>The file path behind a picture URL. Throws if it would be outside the picture root.</summary>
		string MapToPhysicalPath(string url);

		/// <summary>The relative URL of a file under the picture root. Throws if the file is outside it.</summary>
		string ToUrl(string physicalPath);
	}
}
