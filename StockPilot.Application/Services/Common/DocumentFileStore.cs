using System;
using System.Configuration;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Abp;
using Abp.Dependency;

namespace StockPilot.Services.Common
{
	/// <summary>
	/// Purchase invoice documents as files, in the picture folder (StockPilot:PictureRootPath):
	///   {root}\documents\{t{tenantId} | host}\{guid}{extension}
	/// The record keeps the path relative to the root ("documents/t3/9f1c….pdf"). Unlike pictures these are private:
	/// Web.config blocks the documents folder from direct URL access, and they are only downloaded through
	/// PurchaseInvoice.DownloadDocument, which checks the user's permission. Callers validate type, size and content
	/// (PurchaseDocumentRules) before saving.
	/// </summary>
	public class DocumentFileStore : ITransientDependency
	{
		public const string Folder = "documents";

		private static readonly Regex SafeExtension = new Regex("^\\.[a-z0-9]{1,5}$", RegexOptions.IgnoreCase);

		private readonly string _root;

		/// <summary>The documents folder, as a full path without a trailing separator.</summary>
		public string DocumentsPath { get; }

		public DocumentFileStore()
			: this(ConfigurationManager.AppSettings[PictureFileStore.RootPathSetting])
		{
		}

		/// <param name="rootPath">The picture root setting (~/Pictures, a relative or an absolute path), or empty for the default.</param>
		/// <param name="siteRoot">What "~" stands for; the application folder when null.</param>
		public DocumentFileStore(string rootPath, string siteRoot = null)
		{
			_root = PictureFileStore.ResolveRoot(rootPath, siteRoot ?? AppDomain.CurrentDomain.BaseDirectory);
			DocumentsPath = Path.Combine(_root, Folder);
		}

		/// <summary>True for an extension the store keeps: a dot and 1–5 letters or digits.</summary>
		public static bool IsSafeExtension(string extension)
		{
			return extension != null && SafeExtension.IsMatch(extension);
		}

		/// <summary>Writes the document under the tenant's folder with a new name and returns its relative path.</summary>
		/// <param name="extension">The original file's extension (".pdf"); only the extension of the user's name is kept.</param>
		public async Task<string> SaveAsync(int? tenantId, byte[] content, string extension)
		{
			if (content == null || content.Length == 0)
			{
				throw new ArgumentException("The document is empty.", nameof(content));
			}
			if (!IsSafeExtension(extension))
			{
				throw new ArgumentException("Not a document extension: " + extension, nameof(extension));
			}
			string directory = Path.Combine(DocumentsPath, PictureFileStore.TenantFolder(tenantId));
			Directory.CreateDirectory(directory);
			string path = Path.Combine(directory, SequentialGuidGenerator.Instance.Create().ToString("N") + extension.ToLowerInvariant());
			// Written under another name and renamed, so a half-written document is never read.
			string temp = path + ".tmp";
			try
			{
				using (FileStream stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, useAsync: true))
				{
					await stream.WriteAsync(content, 0, content.Length);
				}
				File.Move(temp, path);
			}
			catch
			{
				if (File.Exists(temp))
				{
					File.Delete(temp);
				}
				throw;
			}
			return ToRelativePath(path);
		}

		/// <summary>The document's bytes, or null when the file is missing.</summary>
		public Task<byte[]> ReadOrNullAsync(string relativePath)
		{
			try
			{
				string path = MapToPhysicalPath(relativePath);
				return Task.FromResult(File.Exists(path) ? File.ReadAllBytes(path) : null);
			}
			catch (Exception error)
			{
				return Task.FromException<byte[]>(error);
			}
		}

		/// <summary>Deletes the document. An empty path or a missing file is ignored.</summary>
		public Task DeleteAsync(string relativePath)
		{
			if (string.IsNullOrWhiteSpace(relativePath))
			{
				return Task.CompletedTask;
			}
			try
			{
				string path = MapToPhysicalPath(relativePath);
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

		/// <summary>The full path of a stored document. Throws if the path would leave the documents folder.</summary>
		public string MapToPhysicalPath(string relativePath)
		{
			if (string.IsNullOrWhiteSpace(relativePath))
			{
				throw new ArgumentException("The document path is empty.", nameof(relativePath));
			}
			string clean = relativePath.Trim().Replace('\\', '/');
			if (!clean.StartsWith(Folder + "/", StringComparison.OrdinalIgnoreCase) || Path.IsPathRooted(clean.Substring(Folder.Length + 1)))
			{
				throw new ArgumentException("Not a document path: " + relativePath, nameof(relativePath));
			}
			string path;
			try
			{
				path = Path.GetFullPath(Path.Combine(_root, clean.Replace('/', Path.DirectorySeparatorChar)));
			}
			catch (Exception error) when (error is ArgumentException || error is NotSupportedException || error is PathTooLongException)
			{
				throw new ArgumentException("Not a document path: " + relativePath, nameof(relativePath));
			}
			if (!path.StartsWith(DocumentsPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
			{
				throw new ArgumentException("The document path points outside the documents folder: " + relativePath, nameof(relativePath));
			}
			return path;
		}

		private string ToRelativePath(string fullPath)
		{
			return fullPath.Substring(_root.Length + 1).Replace(Path.DirectorySeparatorChar, '/');
		}
	}
}
