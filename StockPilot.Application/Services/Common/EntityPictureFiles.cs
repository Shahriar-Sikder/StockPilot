using System;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Uow;
using Abp.Threading;
using Castle.Core.Logging;
using StockPilot.Storage;

namespace StockPilot.Services.Common
{
	/// <summary>
	/// The picture of an item, category or sub-category, tied to the current unit of work, so the files always
	/// follow the database:
	///   - a new picture is written at once; if the unit of work then fails, that new file is deleted again;
	///   - the previous picture is deleted only after the unit of work has committed (never before);
	///   - a picture still in the old storage (PictureId, not moved to a file yet) is removed the same way, after
	///     the commit, so a row never ends up with both a file and an old database picture.
	/// Soft delete and restore do not call this: the file stays, so a restored record still shows its picture.
	/// </summary>
	public class EntityPictureFiles : ITransientDependency
	{
		private readonly IPictureFileStore _store;

		private readonly IUnitOfWorkManager _unitOfWorkManager;

		private readonly IBinaryObjectManager _binaryObjectManager;

		public ILogger Logger { get; set; }

		public EntityPictureFiles(IPictureFileStore store, IUnitOfWorkManager unitOfWorkManager, IBinaryObjectManager binaryObjectManager)
		{
			_store = store;
			_unitOfWorkManager = unitOfWorkManager;
			_binaryObjectManager = binaryObjectManager;
			Logger = NullLogger.Instance;
		}

		/// <summary>
		/// Saves the new picture (same checks and resize as before) and returns its URL for the entity. After the
		/// commit the previous file (and an old-storage picture) is deleted; if the commit fails, the new file is.
		/// </summary>
		public async Task<string> ReplaceAsync(int? tenantId, string folder, string base64Content, string previousUrl, Guid? legacyPictureId)
		{
			IActiveUnitOfWork uow = RequireUnitOfWork();
			string url = await _store.SaveAsync(tenantId, folder, base64Content);
			uow.Failed += (sender, args) => DeleteFile(url, "the save failed");
			uow.Completed += (sender, args) => DeleteOld(previousUrl, legacyPictureId);
			return url;
		}

		/// <summary>The entity no longer has a picture: after the commit, its file (and an old-storage picture) is deleted.</summary>
		public void RemoveAfterCommit(string previousUrl, Guid? legacyPictureId)
		{
			if (string.IsNullOrEmpty(previousUrl) && !legacyPictureId.HasValue)
			{
				return;
			}
			RequireUnitOfWork().Completed += (sender, args) => DeleteOld(previousUrl, legacyPictureId);
		}

		private IActiveUnitOfWork RequireUnitOfWork()
		{
			IActiveUnitOfWork uow = _unitOfWorkManager.Current;
			if (uow == null)
			{
				throw new InvalidOperationException("Picture changes need a unit of work (call them from an application service).");
			}
			return uow;
		}

		private void DeleteOld(string previousUrl, Guid? legacyPictureId)
		{
			DeleteFile(previousUrl, "it was replaced or removed");
			if (legacyPictureId.HasValue)
			{
				try
				{
					// The unit of work that changed the entity is finished; the old picture row needs its own.
					using (IUnitOfWorkCompleteHandle cleanup = _unitOfWorkManager.Begin())
					{
						AsyncHelper.RunSync(() => _binaryObjectManager.DeleteAsync(legacyPictureId.Value));
						cleanup.Complete();
					}
				}
				catch (Exception error)
				{
					Logger.Warn("Could not delete the old picture " + legacyPictureId + " from AppBinaryObjects.", error);
				}
			}
		}

		private void DeleteFile(string url, string reason)
		{
			if (string.IsNullOrEmpty(url))
			{
				return;
			}
			try
			{
				AsyncHelper.RunSync(() => _store.DeleteAsync(url));
			}
			catch (Exception error)
			{
				// The database is already right; a file left behind only takes disk space.
				Logger.Warn("Could not delete picture file " + url + " (" + reason + ").", error);
			}
		}
	}
}
