using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Transactions;
using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Threading;
using Castle.Core.Logging;
using StockPilot.Storage;

namespace StockPilot.Services.Common
{
	/// <summary>
	/// One-time move of item, category and sub-category pictures from AppBinaryObjects (PictureId) to files
	/// (PictureUrl, PictureFileStore). Idempotent: it only takes rows with a PictureId and no PictureUrl, so running it
	/// again does nothing. Every tenant and deleted rows are included. Each row is its own transaction: the file is
	/// written first, then PictureUrl is set, PictureId cleared and the BinaryObject deleted in one commit; if that
	/// commit fails, the new file is removed again and the row keeps its old picture. One failed row never stops the
	/// rest. Purchase invoice documents are moved the same way (BinaryObjectId → FilePath, DocumentFileStore), byte
	/// for byte. Logos and profile pictures in AppBinaryObjects are not touched.
	/// Runs at site start (StockPilotWebModule) unless the appSetting StockPilot:PicturesMigratedToFiles is "true".
	/// </summary>
	public class PictureFileMigrator : ITransientDependency
	{
		public const string DoneSetting = "StockPilot:PicturesMigratedToFiles";

		private static readonly string[] AllTenantsAndDeleted = { AbpDataFilters.MayHaveTenant, AbpDataFilters.MustHaveTenant, AbpDataFilters.SoftDelete };

		private readonly IRepository<StockPilot.Entity.ItemMaster> _items;

		private readonly IRepository<StockPilot.Entity.ItemCategory> _categories;

		private readonly IRepository<StockPilot.Entity.ItemSubCategory> _subCategories;

		private readonly IRepository<BinaryObject, Guid> _binaryObjects;

		private readonly IUnitOfWorkManager _unitOfWorkManager;

		private readonly IPictureFileStore _store;

		private readonly IRepository<StockPilot.Entity.PurchaseInvoiceDocument> _documents;

		private readonly DocumentFileStore _documentFiles;

		public ILogger Logger { get; set; }

		public PictureFileMigrator(IRepository<StockPilot.Entity.ItemMaster> items, IRepository<StockPilot.Entity.ItemCategory> categories, IRepository<StockPilot.Entity.ItemSubCategory> subCategories,
			IRepository<BinaryObject, Guid> binaryObjects, IUnitOfWorkManager unitOfWorkManager, IPictureFileStore store,
			IRepository<StockPilot.Entity.PurchaseInvoiceDocument> documents, DocumentFileStore documentFiles)
		{
			_items = items;
			_categories = categories;
			_subCategories = subCategories;
			_binaryObjects = binaryObjects;
			_unitOfWorkManager = unitOfWorkManager;
			_store = store;
			_documents = documents;
			_documentFiles = documentFiles;
			Logger = NullLogger.Instance;
		}

		/// <summary>Site start: runs the move unless the setting says it is done. Never throws.</summary>
		public void RunAtStartup()
		{
			if (string.Equals(ConfigurationManager.AppSettings[DoneSetting], "true", StringComparison.OrdinalIgnoreCase))
			{
				Logger.Debug("Picture and document move to files skipped: " + DoneSetting + " is true.");
				return;
			}
			try
			{
				PictureMoveResult result = AsyncHelper.RunSync(() => MoveAllAsync());
				Logger.Info("Picture move to files: " + result);
			}
			catch (Exception error)
			{
				Logger.Error("Picture move to files could not run; pictures stay in the database until the next start.", error);
			}
			try
			{
				PictureMoveResult result = AsyncHelper.RunSync(() => MoveDocumentsAsync());
				Logger.Info("Document move to files: " + result);
			}
			catch (Exception error)
			{
				Logger.Error("Document move to files could not run; documents stay in the database until the next start.", error);
			}
		}

		/// <summary>Purchase invoice documents: rows with a BinaryObjectId and no FilePath.</summary>
		public async Task<PictureMoveResult> MoveDocumentsAsync()
		{
			PictureMoveResult result = new PictureMoveResult();
			List<int> ids;
			using (IUnitOfWorkCompleteHandle uow = _unitOfWorkManager.Begin(new UnitOfWorkOptions { Scope = TransactionScopeOption.RequiresNew }))
			using (_unitOfWorkManager.Current.DisableFilter(AllTenantsAndDeleted))
			{
				ids = _documents.GetAll().Where(d => d.BinaryObjectId != null && d.FilePath == null).Select(d => d.Id).ToList();
				foreach (int id in _documents.GetAll().Where(d => d.BinaryObjectId != null && d.FilePath != null).Select(d => d.Id).ToList())
				{
					result.Skipped++;
					Logger.Warn($"Document move: document {id} has both BinaryObjectId and FilePath; skipped, check it by hand.");
				}
				await uow.CompleteAsync();
			}
			foreach (int id in ids)
			{
				await MoveDocumentAsync(id, result);
			}
			return result;
		}

		private async Task MoveDocumentAsync(int id, PictureMoveResult result)
		{
			string filePath = null;
			try
			{
				using (IUnitOfWorkCompleteHandle uow = _unitOfWorkManager.Begin(new UnitOfWorkOptions { Scope = TransactionScopeOption.RequiresNew }))
				using (_unitOfWorkManager.Current.DisableFilter(AllTenantsAndDeleted))
				{
					StockPilot.Entity.PurchaseInvoiceDocument document = await _documents.FirstOrDefaultAsync(id);
					if (document?.BinaryObjectId == null || document.FilePath != null)
					{
						result.Skipped++; // changed since it was listed
						await uow.CompleteAsync();
						return;
					}
					BinaryObject binary = await _binaryObjects.FirstOrDefaultAsync(document.BinaryObjectId.Value);
					if (binary == null || binary.Bytes == null || binary.Bytes.Length == 0)
					{
						Logger.Warn($"Document move: document {id} pointed to content {document.BinaryObjectId} which does not exist; BinaryObjectId cleared.");
						document.BinaryObjectId = null;
						await uow.CompleteAsync();
						result.Missing++;
						return;
					}
					filePath = await _documentFiles.SaveAsync(document.TenantId, binary.Bytes, DocumentExtension(document.FileName));
					document.FilePath = filePath;
					document.BinaryObjectId = null;
					await _binaryObjects.DeleteAsync(binary);
					await uow.CompleteAsync();
				}
				result.Moved++;
			}
			catch (Exception error)
			{
				result.Failed++;
				Logger.Error($"Document move: document {id} failed; it stays in the database.", error);
				if (filePath != null)
				{
					try
					{
						await _documentFiles.DeleteAsync(filePath);
					}
					catch (Exception cleanup)
					{
						Logger.Warn("Document move: could not remove " + filePath + " after the failure.", cleanup);
					}
				}
			}
		}

		/// <summary>The extension of the uploaded name; ".bin" if it has none the store accepts (the name is kept on the row).</summary>
		private static string DocumentExtension(string fileName)
		{
			string extension = Path.GetExtension(fileName ?? string.Empty);
			return DocumentFileStore.IsSafeExtension(extension) ? extension : ".bin";
		}

		public async Task<PictureMoveResult> MoveAllAsync()
		{
			PictureMoveResult result = new PictureMoveResult();
			await MoveAsync(_items, PictureFolders.Items, "ItemMaster",
				e => e.PictureId != null && e.PictureUrl == null, e => e.PictureId != null && e.PictureUrl != null,
				e => e.TenantId, e => e.PictureId, (e, url) => { e.PictureUrl = url; e.PictureId = null; }, result);
			await MoveAsync(_categories, PictureFolders.Categories, "ItemCategory",
				e => e.PictureId != null && e.PictureUrl == null, e => e.PictureId != null && e.PictureUrl != null,
				e => e.TenantId, e => e.PictureId, (e, url) => { e.PictureUrl = url; e.PictureId = null; }, result);
			await MoveAsync(_subCategories, PictureFolders.SubCategories, "ItemSubCategory",
				e => e.PictureId != null && e.PictureUrl == null, e => e.PictureId != null && e.PictureUrl != null,
				e => e.TenantId, e => e.PictureId, (e, url) => { e.PictureUrl = url; e.PictureId = null; }, result);
			return result;
		}

		/// <param name="pending">Rows to move: PictureId set, PictureUrl empty.</param>
		/// <param name="conflicting">Rows with both set (not expected); skipped and logged for a manual look.</param>
		/// <param name="moveTo">Sets PictureUrl and clears PictureId (an empty url only clears PictureId).</param>
		private async Task MoveAsync<TEntity>(IRepository<TEntity> repository, string folder, string kind,
			Expression<Func<TEntity, bool>> pending, Expression<Func<TEntity, bool>> conflicting,
			Func<TEntity, int> tenantOf, Func<TEntity, Guid?> pictureOf, Action<TEntity, string> moveTo, PictureMoveResult result)
			where TEntity : class, IEntity<int>
		{
			List<int> ids;
			List<int> conflictIds;
			using (IUnitOfWorkCompleteHandle uow = _unitOfWorkManager.Begin(new UnitOfWorkOptions { Scope = TransactionScopeOption.RequiresNew }))
			using (_unitOfWorkManager.Current.DisableFilter(AllTenantsAndDeleted))
			{
				ids = repository.GetAll().Where(pending).Select(e => e.Id).ToList();
				conflictIds = repository.GetAll().Where(conflicting).Select(e => e.Id).ToList();
				await uow.CompleteAsync();
			}
			foreach (int id in conflictIds)
			{
				result.Skipped++;
				Logger.Warn($"Picture move: {kind} {id} has both PictureId and PictureUrl; skipped, check it by hand.");
			}
			foreach (int id in ids)
			{
				await MoveOneAsync(repository, folder, kind, id, tenantOf, pictureOf, moveTo, result);
			}
		}

		private async Task MoveOneAsync<TEntity>(IRepository<TEntity> repository, string folder, string kind, int id,
			Func<TEntity, int> tenantOf, Func<TEntity, Guid?> pictureOf, Action<TEntity, string> moveTo, PictureMoveResult result)
			where TEntity : class, IEntity<int>
		{
			string url = null;
			try
			{
				using (IUnitOfWorkCompleteHandle uow = _unitOfWorkManager.Begin(new UnitOfWorkOptions { Scope = TransactionScopeOption.RequiresNew }))
				using (_unitOfWorkManager.Current.DisableFilter(AllTenantsAndDeleted))
				{
					TEntity entity = await repository.FirstOrDefaultAsync(id);
					Guid? pictureId = entity == null ? null : pictureOf(entity);
					if (!pictureId.HasValue)
					{
						result.Skipped++; // changed since it was listed
						await uow.CompleteAsync();
						return;
					}
					BinaryObject binary = await _binaryObjects.FirstOrDefaultAsync(pictureId.Value);
					if (binary == null)
					{
						// The picture was already gone: nothing to move, so only the dangling PictureId is cleared.
						moveTo(entity, null);
						await uow.CompleteAsync();
						result.Missing++;
						Logger.Warn($"Picture move: {kind} {id} pointed to picture {pictureId} which does not exist; PictureId cleared.");
						return;
					}
					// The file first; the row and the BinaryObject change only if it was written.
					url = await _store.SaveAsync(tenantOf(entity), folder, Convert.ToBase64String(binary.Bytes));
					moveTo(entity, url);
					await _binaryObjects.DeleteAsync(binary);
					await uow.CompleteAsync();
				}
				result.Moved++;
			}
			catch (Exception error)
			{
				result.Failed++;
				Logger.Error($"Picture move: {kind} {id} failed; it keeps its picture in the database.", error);
				if (url != null)
				{
					try
					{
						await _store.DeleteAsync(url); // the commit failed, so the new file has no owner
					}
					catch (Exception cleanup)
					{
						Logger.Warn("Picture move: could not remove " + url + " after the failure.", cleanup);
					}
				}
			}
		}
	}

	public class PictureMoveResult
	{
		/// <summary>Pictures written to files (row updated, BinaryObject deleted).</summary>
		public int Moved { get; set; }

		/// <summary>Rows not touched: both columns set, or changed while the move ran.</summary>
		public int Skipped { get; set; }

		/// <summary>Rows whose PictureId pointed to nothing; PictureId was cleared.</summary>
		public int Missing { get; set; }

		/// <summary>Rows that could not be moved (bad picture data, file or database error); they keep PictureId.</summary>
		public int Failed { get; set; }

		public override string ToString()
		{
			return $"moved {Moved}, skipped {Skipped}, missing {Missing}, failed {Failed}";
		}
	}
}
