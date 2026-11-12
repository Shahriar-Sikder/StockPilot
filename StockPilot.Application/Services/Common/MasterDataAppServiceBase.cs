using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using System.Threading.Tasks;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Abp.Domain.Repositories;
using Abp.UI;

namespace StockPilot.Services.Common
{
	public abstract class MasterDataAppServiceBase : StockPilotAppServiceBase
	{
		private class NullDisposable : IDisposable
		{
			public void Dispose()
			{
			}
		}

		public const int MaxExportRows = 100000;

		protected TEntity MapForCreate<TEntity>(object input) where TEntity : FullAuditedEntity
		{
			TEntity val = base.ObjectMapper.Map<TEntity>(input);
			val.Id = 0;
			val.CreationTime = default(DateTime);
			val.CreatorUserId = null;
			val.LastModificationTime = null;
			val.LastModifierUserId = null;
			val.IsDeleted = false;
			val.DeleterUserId = null;
			val.DeletionTime = null;
			return val;
		}

		protected void MapForUpdate<TEntity>(object input, TEntity entity) where TEntity : FullAuditedEntity, IMustHaveTenant
		{
			int id = entity.Id;
			int tenantId = entity.TenantId;
			DateTime creationTime = entity.CreationTime;
			long? creatorUserId = entity.CreatorUserId;
			DateTime? lastModificationTime = entity.LastModificationTime;
			long? lastModifierUserId = entity.LastModifierUserId;
			base.ObjectMapper.Map(input, entity);
			entity.Id = id;
			entity.TenantId = tenantId;
			entity.CreationTime = creationTime;
			entity.CreatorUserId = creatorUserId;
			entity.LastModificationTime = lastModificationTime;
			entity.LastModifierUserId = lastModifierUserId;
			entity.IsDeleted = false;
		}

		protected IDisposable IncludeDeleted(bool includeDeleted)
		{
			if (!includeDeleted)
			{
				return new NullDisposable();
			}
			return base.CurrentUnitOfWork.DisableFilter("SoftDelete");
		}

		protected static IQueryable<T> ApplySorting<T>(IQueryable<T> query, string sorting, string defaultSorting, IDictionary<string, string> allowedFields)
		{
			return query.OrderBy(TranslateSorting(sorting, defaultSorting, allowedFields));
		}

		public static string TranslateSorting(string sorting, string defaultSorting, IDictionary<string, string> allowedFields)
		{
			if (string.IsNullOrWhiteSpace(sorting))
			{
				return defaultSorting;
			}
			string[] parts = sorting.Trim().Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length > 2)
			{
				return defaultSorting;
			}
			KeyValuePair<string, string> match = allowedFields.FirstOrDefault((KeyValuePair<string, string> pair) => string.Equals(pair.Key, parts[0], StringComparison.OrdinalIgnoreCase));
			if (match.Key == null)
			{
				return defaultSorting;
			}
			bool descending = parts.Length == 2 && string.Equals(parts[1], "desc", StringComparison.OrdinalIgnoreCase);
			if (parts.Length == 2 && !descending && !string.Equals(parts[1], "asc", StringComparison.OrdinalIgnoreCase))
			{
				return defaultSorting;
			}
			return match.Value + (descending ? " descending" : " ascending");
		}

		protected async Task RestoreEntityAsync<TEntity>(IRepository<TEntity> repository, int id, Func<TEntity, Task> beforeRestore = null) where TEntity : FullAuditedEntity
		{
			TEntity entity;
			using (base.CurrentUnitOfWork.DisableFilter("SoftDelete"))
			{
				entity = await repository.FirstOrDefaultAsync(id);
			}
			if (entity == null)
			{
				throw new UserFriendlyException(L("RecordNotFound"));
			}
			if (entity.IsDeleted)
			{
				if (beforeRestore != null)
				{
					await beforeRestore(entity);
				}
				entity.IsDeleted = false;
				entity.DeleterUserId = null;
				entity.DeletionTime = null;
			}
		}

		protected UserFriendlyException CodeExists(string code)
		{
			return new UserFriendlyException(L("CodeAlreadyExists", code));
		}

		protected UserFriendlyException InUse(string name, string usedBy)
		{
			return new UserFriendlyException(L("RecordInUseCannotDelete", name, usedBy));
		}
	}
}
