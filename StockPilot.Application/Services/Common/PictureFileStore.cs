using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using Abp;
using Abp.Dependency;
using Abp.UI;

namespace StockPilot.Services.Common
{
	/// <summary>
	/// Stores item, category and sub-category pictures as files:
	///   {root}\{t{tenantId} | host}\{items | categories | sub-categories}\{guid}.png
	/// and hands out the relative URL {baseUrl}{tenant folder}/{folder}/{guid}.png.
	/// Settings (Web.config appSettings): StockPilot:PictureRootPath (~/Pictures or an absolute folder) and
	/// StockPilot:PictureBaseUrl (/Pictures/).
	/// </summary>
	public class PictureFileStore : StockPilotServiceBase, IPictureFileStore, ITransientDependency
	{
		public const string RootPathSetting = "StockPilot:PictureRootPath";

		public const string BaseUrlSetting = "StockPilot:PictureBaseUrl";

		public const string DefaultRootPath = "~/Pictures";

		public const string DefaultBaseUrl = "/Pictures/";

		public const string HostFolder = "host";

		private const string Extension = ".png";

		private readonly string _root;

		private readonly string _baseUrl;

		/// <summary>The picture folder, as a full path without a trailing separator.</summary>
		public string RootPath => _root;

		/// <summary>The URL prefix: starts and ends with '/'.</summary>
		public string BaseUrl => _baseUrl;

		public PictureFileStore()
			: this(ConfigurationManager.AppSettings[RootPathSetting], ConfigurationManager.AppSettings[BaseUrlSetting])
		{
		}

		/// <param name="rootPath">A virtual path (~/Pictures), an absolute path, or empty for the default.</param>
		/// <param name="baseUrl">The URL prefix, or empty for the default.</param>
		/// <param name="siteRoot">What "~" stands for; the application folder when null.</param>
		public PictureFileStore(string rootPath, string baseUrl, string siteRoot = null)
		{
			_root = ResolveRoot(rootPath, siteRoot ?? AppDomain.CurrentDomain.BaseDirectory);
			_baseUrl = NormalizeBaseUrl(baseUrl);
		}

		/// <summary>The folder of a tenant's pictures: t{tenantId}, or host without a tenant.</summary>
		public static string TenantFolder(int? tenantId)
		{
			return tenantId.HasValue ? "t" + tenantId.Value : HostFolder;
		}

		public async Task<string> SaveAsync(int? tenantId, string folder, string base64Content)
		{
			if (!PictureFolders.IsKnown(folder))
			{
				throw new ArgumentException("Unknown picture folder: " + folder, nameof(folder));
			}
			byte[] png = ToPng(base64Content);
			string directory = Path.Combine(_root, TenantFolder(tenantId), folder);
			Directory.CreateDirectory(directory);
			// The name is ours (never the uploaded file's name), so it is unique and safe as a path.
			string path = Path.Combine(directory, SequentialGuidGenerator.Instance.Create().ToString("N") + Extension);
			// Written under another name and renamed, so a half-written picture is never served.
			string temp = path + ".tmp";
			try
			{
				using (FileStream stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: true))
				{
					await stream.WriteAsync(png, 0, png.Length);
				}
				File.Move(temp, path);
			}
			catch
			{
				TryDelete(temp);
				throw;
			}
			return ToUrl(path);
		}

		public async Task<string> ReplaceAsync(int? tenantId, string folder, string previousPictureUrl, string base64Content)
		{
			string url = await SaveAsync(tenantId, folder, base64Content);
			await DeleteAsync(previousPictureUrl);
			return url;
		}

		public Task DeleteAsync(string pictureUrl)
		{
			if (string.IsNullOrWhiteSpace(pictureUrl))
			{
				return Task.CompletedTask;
			}
			// A refused URL (outside the root) or a failed delete comes back through the task, like any awaited call.
			try
			{
				string path = MapToPhysicalPath(pictureUrl);
				if (File.Exists(path))
				{
					File.Delete(path);
				}
				return Task.CompletedTask;
			}
			catch (Exception error)
			{
				return Task.FromException(error);
			}
		}

		public string MapToPhysicalPath(string url)
		{
			if (string.IsNullOrWhiteSpace(url))
			{
				throw new ArgumentException("The picture URL is empty.", nameof(url));
			}
			string clean = url.Trim();
			int cut = clean.IndexOfAny(new[] { '?', '#' });
			if (cut >= 0)
			{
				clean = clean.Substring(0, cut);
			}
			if (!clean.StartsWith(_baseUrl, StringComparison.OrdinalIgnoreCase))
			{
				throw new ArgumentException("Not a picture URL: " + url, nameof(url));
			}
			string relative = clean.Substring(_baseUrl.Length).Replace('/', Path.DirectorySeparatorChar);
			string path;
			try
			{
				path = Path.GetFullPath(Path.Combine(_root, relative));
			}
			catch (Exception error) when (error is ArgumentException || error is NotSupportedException || error is PathTooLongException)
			{
				throw new ArgumentException("Not a picture URL: " + url, nameof(url));
			}
			// GetFullPath resolved any "..": the result must still be a .png inside the root.
			if (!IsUnderRoot(path) || !string.Equals(Path.GetExtension(path), Extension, StringComparison.OrdinalIgnoreCase))
			{
				throw new ArgumentException("The picture URL points outside the picture folder: " + url, nameof(url));
			}
			return path;
		}

		public string ToUrl(string physicalPath)
		{
			if (string.IsNullOrWhiteSpace(physicalPath))
			{
				throw new ArgumentException("The path is empty.", nameof(physicalPath));
			}
			string path = Path.GetFullPath(physicalPath);
			if (!IsUnderRoot(path))
			{
				throw new ArgumentException("The file is outside the picture folder: " + physicalPath, nameof(physicalPath));
			}
			return _baseUrl + path.Substring(_root.Length + 1).Replace(Path.DirectorySeparatorChar, '/');
		}

		/// <summary>Decode, check and resize exactly as PictureStore does, with the same messages.</summary>
		private byte[] ToPng(string base64Content)
		{
			byte[] bytes = PictureHelper.DecodeBase64(base64Content);
			if (bytes == null || bytes.Length == 0)
			{
				throw new UserFriendlyException(L("InvalidPictureFile"));
			}
			if (bytes.Length > PictureHelper.MaxUploadBytes)
			{
				throw new UserFriendlyException(L("PictureTooLarge", 3));
			}
			byte[] resized = PictureHelper.ResizeToPng(bytes);
			if (resized == null)
			{
				throw new UserFriendlyException(L("InvalidPictureFile"));
			}
			return resized;
		}

		private bool IsUnderRoot(string fullPath)
		{
			return fullPath.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>The full path of the picture folder: a virtual path (~/Pictures), a path relative to the site, or an absolute one.</summary>
		public static string ResolveRoot(string rootPath, string siteRoot)
		{
			string value = string.IsNullOrWhiteSpace(rootPath) ? DefaultRootPath : rootPath.Trim();
			if (value.StartsWith("~"))
			{
				// ~/Pictures: inside the site.
				value = Path.Combine(siteRoot, value.Substring(1).TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar));
			}
			else if (!Path.IsPathRooted(value))
			{
				value = Path.Combine(siteRoot, value);
			}
			return Path.GetFullPath(value).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		}

		private static string NormalizeBaseUrl(string baseUrl)
		{
			string value = string.IsNullOrWhiteSpace(baseUrl) ? DefaultBaseUrl : baseUrl.Trim().Replace('\\', '/');
			if (value.StartsWith("~"))
			{
				value = value.Substring(1);
			}
			// Relative to the site on purpose: an absolute http://host:port prefix would break when the address changes.
			if (value.Contains("://"))
			{
				throw new ConfigurationErrorsException(BaseUrlSetting + " must be a relative URL such as /Pictures/, not " + baseUrl);
			}
			return "/" + value.Trim('/') + "/";
		}

		private static void TryDelete(string path)
		{
			try
			{
				if (File.Exists(path))
				{
					File.Delete(path);
				}
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
		}
	}
}
