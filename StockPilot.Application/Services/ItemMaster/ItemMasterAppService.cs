using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Features;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.ObjectMapping;
using Abp.UI;
using StockPilot.Dto;
using StockPilot.Entity;
using StockPilot.Inventory;
using StockPilot.Services.Common;
using StockPilot.Services.Common.Dtos;
using StockPilot.Services.ItemMaster.Dtos;
using StockPilot.Services.ItemMaster.Exporting;

namespace StockPilot.Services.ItemMaster
{
	[AbpAuthorize(new string[] { })]
	public class ItemMasterAppService : MasterDataAppServiceBase, IItemMasterAppService, IApplicationService, ITransientDependency
	{
		private static readonly Dictionary<string, string> SortFields = new Dictionary<string, string>
		{
			{ "itemCode", "ItemCode" },
			{ "itemName", "ItemName" },
			{ "barcode", "Barcode" },
			{ "itemCategoryName", "ItemCategoryName" },
			{ "itemSubCategoryName", "ItemSubCategoryName" },
			{ "baseUnitCode", "BaseUnitCode" },
			{ "quantityOnHand", "QuantityOnHand" },
			{ "averageCost", "AverageCost" },
			{ "stockValue", "StockValue" },
			{ "levelRatio", "LevelRatio" },
			{ "status", "StatusCode" },
			{ "salePrice", "SalePrice" },
			{ "reorderLevel", "ReorderLevel" },
			{ "isActive", "IsActive" },
			{ "creationTime", "CreationTime" }
		};

		private const int RecentMovementCount = 6;

		private readonly IRepository<StockPilot.Entity.ItemMaster> _itemRepository;

		private readonly IRepository<StockBalance> _balanceRepository;

		private readonly IRepository<StockPilot.Entity.StockMovement, long> _movementRepository;

		private readonly IRepository<StockPilot.Entity.ItemCategory> _categoryRepository;

		private readonly IRepository<StockPilot.Entity.ItemSubCategory> _subCategoryRepository;

		private readonly IRepository<StockPilot.Entity.UnitSet> _unitSetRepository;

		private readonly IRepository<StockPilot.Entity.UnitMaster> _unitRepository;

		private readonly IRepository<StockPilot.Entity.Supplier> _supplierRepository;

		private readonly IRepository<PurchaseInvoiceItem> _purchaseLineRepository;

		private readonly IRepository<IssueSalesItem> _saleLineRepository;

		private readonly UnitConversionManager _unitConversionManager;

		private readonly StockManager _stockManager;

		private readonly PictureStore _pictureStore;

		public ItemMasterExcelExporter ExcelExporter { get; set; }

		public ItemMasterAppService(IRepository<StockPilot.Entity.ItemMaster> itemRepository, IRepository<StockBalance> balanceRepository, IRepository<StockPilot.Entity.StockMovement, long> movementRepository, IRepository<StockPilot.Entity.ItemCategory> categoryRepository, IRepository<StockPilot.Entity.ItemSubCategory> subCategoryRepository, IRepository<StockPilot.Entity.UnitSet> unitSetRepository, IRepository<StockPilot.Entity.UnitMaster> unitRepository, IRepository<StockPilot.Entity.Supplier> supplierRepository, IRepository<PurchaseInvoiceItem> purchaseLineRepository, IRepository<IssueSalesItem> saleLineRepository, UnitConversionManager unitConversionManager, StockManager stockManager, PictureStore pictureStore)
		{
			_itemRepository = itemRepository;
			_balanceRepository = balanceRepository;
			_movementRepository = movementRepository;
			_categoryRepository = categoryRepository;
			_subCategoryRepository = subCategoryRepository;
			_unitSetRepository = unitSetRepository;
			_unitRepository = unitRepository;
			_supplierRepository = supplierRepository;
			_purchaseLineRepository = purchaseLineRepository;
			_saleLineRepository = saleLineRepository;
			_unitConversionManager = unitConversionManager;
			_stockManager = stockManager;
			_pictureStore = pictureStore;
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Items" })]
		public async Task<ItemMasterPagedResultDto> GetAll(ItemMasterGridInput input)
		{
			using (IncludeDeleted(input.IncludeDeleted))
			{
				IQueryable<ItemMasterGridRow> baseQuery = FilteredQuery(input);
				var statusCounts = await (from r in baseQuery
					group r by r.StatusCode into g
					select new
					{
						StatusCode = g.Key,
						Count = g.Count()
					}).ToListAsync();
				IQueryable<ItemMasterGridRow> query = ApplyStatus(baseQuery, input.Status);
				int totalCount = await query.CountAsync();
				List<ItemMasterGridRow> rows = await MasterDataAppServiceBase.ApplySorting(query, input.Sorting, "ItemName ascending", SortFields).PageBy(input).ToListAsync();
				return new ItemMasterPagedResultDto(totalCount, base.ObjectMapper.Map<List<ItemMasterListDto>>(rows))
				{
					AllCount = statusCounts.Sum(c => c.Count),
					InStockCount = statusCounts.Where(c => c.StatusCode == 0).Sum(c => c.Count),
					NeedsReorderCount = statusCounts.Where(c => c.StatusCode == 1 || c.StatusCode == 2).Sum(c => c.Count)
				};
			}
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Items" })]
		[RequiresFeature(new string[] { "App.Inventory.Export" })]
		public async Task<FileDto> GetAllToExcel(ItemMasterGridInput input)
		{
			using (IncludeDeleted(input.IncludeDeleted))
			{
				List<ItemMasterGridRow> rows = await MasterDataAppServiceBase.ApplySorting(ApplyStatus(FilteredQuery(input), input.Status), input.Sorting, "ItemName ascending", SortFields).Take(100000).ToListAsync();
				return ExcelExporter.Export(base.ObjectMapper.Map<List<ItemMasterListDto>>(rows));
			}
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Items" })]
		public async Task<ItemDetailDto> GetItemDetail(EntityDto input)
		{
			StockPilot.Entity.ItemMaster item = await _itemRepository.GetAll().Include((StockPilot.Entity.ItemMaster i) => i.ItemCategoryInfo).Include((StockPilot.Entity.ItemMaster i) => i.ItemSubCategoryInfo)
				.Include((StockPilot.Entity.ItemMaster i) => i.BaseUnit)
				.FirstOrDefaultAsync((StockPilot.Entity.ItemMaster i) => i.Id == input.Id);
			if (item == null)
			{
				throw new EntityNotFoundException(typeof(StockPilot.Entity.ItemMaster), input.Id);
			}
			StockBalance balance = await _balanceRepository.FirstOrDefaultAsync((StockBalance b) => b.ItemMasterId == item.Id);
			decimal quantity = balance?.QuantityOnHand ?? 0m;
			decimal average = balance?.AverageCost ?? 0m;
			List<StockPilot.Entity.StockMovement> movements = await (from m in _movementRepository.GetAll()
				where m.ItemMasterId == item.Id
				orderby m.Id descending
				select m).Take(6).ToListAsync();
			ItemDetailDto itemDetailDto = new ItemDetailDto
			{
				Id = item.Id,
				ItemCode = item.ItemCode,
				ItemName = item.ItemName,
				Description = item.Description,
				Barcode = item.Barcode,
				ItemCategoryName = item.ItemCategoryInfo?.Name,
				ItemSubCategoryName = item.ItemSubCategoryInfo?.Name,
				PictureId = item.PictureId,
				IsStockItem = item.IsStockItem,
				IsActive = item.IsActive,
				Status = ItemStockRules.GetStatus(item.IsStockItem, quantity, item.ReorderLevel),
				QuantityOnHand = quantity,
				ReservedQuantity = (balance?.ReservedQuantity ?? 0m),
				AvailableQuantity = (balance?.AvailableQuantity ?? 0m),
				AverageCost = average,
				LastPurchaseCost = (balance?.LastPurchaseCost ?? 0m),
				StockValue = StockManager.Round(quantity * average),
				StandardCost = item.StandardCost,
				SalePrice = item.SalePrice,
				ReorderLevel = item.ReorderLevel,
				LevelRatio = ItemStockRules.GetLevelRatio(quantity, item.ReorderLevel),
				BaseUnitId = item.BaseUnitId,
				BaseUnitCode = item.BaseUnit?.Code,
				BaseUnitName = item.BaseUnit?.Name
			};
			ItemDetailDto itemDetailDto2 = itemDetailDto;
			IObjectMapper objectMapper = base.ObjectMapper;
			itemDetailDto2.Units = objectMapper.Map<List<ItemUnitDto>>(await _unitConversionManager.GetUnitsForItemAsync(item.Id));
			itemDetailDto.RecentMovements = base.ObjectMapper.Map<List<ItemMovementDto>>(movements);
			itemDetailDto.CreationTime = item.CreationTime;
			itemDetailDto.LastModificationTime = item.LastModificationTime;
			return itemDetailDto;
		}

		public async Task<ListResultDto<ItemSearchResultDto>> SearchForDocument(SearchItemsInput input)
		{
			string text = input.Text?.Trim() ?? string.Empty;
			return new ListResultDto<ItemSearchResultDto>(await (from i in _itemRepository.GetAll()
				where i.IsActive && (i.ItemName.Contains(text) || i.ItemCode.Contains(text) || i.Barcode.Contains(text))
				join b in _balanceRepository.GetAll() on i.Id equals b.ItemMasterId into balances
				from b in balances.DefaultIfEmpty()
				orderby (i.ItemCode == text || i.Barcode == text) ? 0 : ((i.ItemName.StartsWith(text) || i.ItemCode.StartsWith(text)) ? 1 : 2), i.ItemName
				select new ItemSearchResultDto
				{
					Id = i.Id,
					ItemCode = i.ItemCode,
					ItemName = i.ItemName,
					Barcode = i.Barcode,
					PictureId = i.PictureId,
					IsStockItem = i.IsStockItem,
					QuantityOnHand = (((decimal?)b.QuantityOnHand) ?? 0m),
					AvailableQuantity = (((decimal?)b.QuantityOnHand) ?? 0m) - (((decimal?)b.ReservedQuantity) ?? 0m),
					BaseUnitId = i.BaseUnitId,
					BaseUnitCode = i.BaseUnit.Code,
					PurchaseUnitId = i.PurchaseUnitId,
					SalesUnitId = i.SalesUnitId,
					StandardCost = i.StandardCost,
					LastPurchaseCost = (((decimal?)b.LastPurchaseCost) ?? 0m),
					SalePrice = i.SalePrice,
					TaxPercent = i.TaxPercent
				}).Take(input.MaxResultCount).ToListAsync());
		}

		public async Task<ListResultDto<ItemMasterComboDto>> GetForCombo()
		{
			List<StockPilot.Entity.ItemMaster> rows = await (from i in _itemRepository.GetAll()
				where i.IsActive
				orderby i.ItemName
				select i).ToListAsync();
			return new ListResultDto<ItemMasterComboDto>(base.ObjectMapper.Map<List<ItemMasterComboDto>>(rows));
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Items" })]
		public async Task<EditItemMasterDto> GetForEdit(EntityDto input)
		{
			IObjectMapper objectMapper = base.ObjectMapper;
			return objectMapper.Map<EditItemMasterDto>(await _itemRepository.GetAsync(input.Id));
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Items.Create" })]
		public async Task<EntityDto> Create(CreateItemMasterDto input)
		{
			input.ItemCode = input.ItemCode.Trim();
			await EnsureCodeIsFreeAsync(input.ItemCode, null);
			await CheckPictureFeatureAsync(input.Picture != null);
			StockPilot.Entity.ItemMaster entity = MapForCreate<StockPilot.Entity.ItemMaster>(input);
			entity.PictureId = null;
			await ValidateAsync(entity);
			if (entity.IsActive)
			{
				await CheckItemLimitAsync(null);
			}
			if (input.Picture != null)
			{
				StockPilot.Entity.ItemMaster itemMaster = entity;
				itemMaster.PictureId = await _pictureStore.ReplaceAsync(null, input.Picture);
			}
			int id = await _itemRepository.InsertAndGetIdAsync(entity);
			await _stockManager.EnsureBalanceAsync(id);
			return new EntityDto(id);
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Items.Edit" })]
		public async Task Update(EditItemMasterDto input)
		{
			input.ItemCode = input.ItemCode.Trim();
			StockPilot.Entity.ItemMaster entity = await _itemRepository.GetAsync(input.Id);
			await EnsureCodeIsFreeAsync(input.ItemCode, entity.Id);
			bool flag = input.UnitSetId != entity.UnitSetId || input.BaseUnitId != entity.BaseUnitId || input.IsStockItem != entity.IsStockItem;
			if (flag)
			{
				flag = await HasMovementsAsync(entity.Id);
			}
			if (flag)
			{
				throw new UserFriendlyException(L("ItemStockSettingsLockedAfterMovements", entity.ItemName));
			}
			await CheckPictureFeatureAsync(input.Picture != null || input.RemovePicture);
			if (input.IsActive && !entity.IsActive)
			{
				await CheckItemLimitAsync(entity.Id);
			}
			Guid? pictureId = entity.PictureId;
			MapForUpdate(input, entity);
			entity.PictureId = pictureId;
			await ValidateAsync(entity);
			if (input.Picture != null)
			{
				StockPilot.Entity.ItemMaster itemMaster = entity;
				itemMaster.PictureId = await _pictureStore.ReplaceAsync(entity.PictureId, input.Picture);
			}
			else if (input.RemovePicture)
			{
				await _pictureStore.DeleteAsync(entity.PictureId);
				entity.PictureId = null;
			}
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Items.Delete" })]
		public async Task Delete(EntityDto input)
		{
			StockPilot.Entity.ItemMaster entity = await _itemRepository.GetAsync(input.Id);
			if (await HasMovementsAsync(entity.Id))
			{
				throw new UserFriendlyException(L("ItemHasStockHistory", entity.ItemName));
			}
			int id = entity.Id;
			if (await _purchaseLineRepository.GetAll().AnyAsync((PurchaseInvoiceItem l) => l.ItemMasterId == id && !l.PurchaseInvoiceInfo.IsDeleted && (int)l.PurchaseInvoiceInfo.Status != 2) | await _saleLineRepository.GetAll().AnyAsync((IssueSalesItem l) => l.ItemMasterId == id && !l.IssueSaleInfo.IsDeleted && (int)l.IssueSaleInfo.Status != 2))
			{
				throw new UserFriendlyException(L("ItemOnOpenDocuments", entity.ItemName));
			}
			await _itemRepository.DeleteAsync(entity);
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Items.Delete" })]
		public async Task Restore(EntityDto input)
		{
			await RestoreEntityAsync(_itemRepository, input.Id, async delegate(StockPilot.Entity.ItemMaster entity)
			{
				await EnsureCodeIsFreeAsync(entity.ItemCode, entity.Id);
				await ValidateAsync(entity);
				if (entity.IsActive)
				{
					await CheckItemLimitAsync(entity.Id);
				}
			});
		}

		private async Task CheckItemLimitAsync(int? exceptItemId)
		{
			int limit = (await base.FeatureChecker.GetValueAsync("App.MaxItemCount")).To<int>();
			if (limit <= 0 || await _itemRepository.CountAsync((StockPilot.Entity.ItemMaster i) => i.IsActive && (exceptItemId == (int?)null || (int?)i.Id != exceptItemId)) < limit)
			{
				return;
			}
			throw new UserFriendlyException(L("MaximumItemCount_Error_Message", limit));
		}

		private async Task CheckPictureFeatureAsync(bool changesPicture)
		{
			bool flag = changesPicture;
			if (flag)
			{
				flag = !(await base.FeatureChecker.IsEnabledAsync("App.Inventory.Pictures"));
			}
			if (flag)
			{
				throw new UserFriendlyException(L("FeatureNotEnabledForStore"));
			}
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Items.Edit" })]
		[RequiresFeature(new string[] { "App.Inventory.Pictures" })]
		public async Task<PictureDto> UploadPicture(UploadPictureInput input)
		{
			StockPilot.Entity.ItemMaster entity = await _itemRepository.GetAsync(input.Id);
			StockPilot.Entity.ItemMaster itemMaster = entity;
			itemMaster.PictureId = await _pictureStore.ReplaceAsync(entity.PictureId, input);
			return await _pictureStore.GetOrNullAsync(entity.PictureId);
		}

		public async Task<PictureDto> GetPicture(EntityDto input)
		{
			StockPilot.Entity.ItemMaster entity = await _itemRepository.GetAsync(input.Id);
			return await _pictureStore.GetOrNullAsync(entity.PictureId);
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Items.Edit" })]
		[RequiresFeature(new string[] { "App.Inventory.Pictures" })]
		public async Task DeletePicture(EntityDto input)
		{
			StockPilot.Entity.ItemMaster entity = await _itemRepository.GetAsync(input.Id);
			await _pictureStore.DeleteAsync(entity.PictureId);
			entity.PictureId = null;
		}

		private IQueryable<ItemMasterGridRow> FilteredQuery(ItemMasterGridInput input)
		{
			return ItemMasterGridQuery.Build(_itemRepository.GetAll(), _balanceRepository.GetAll()).WhereIf(!input.Filter.IsNullOrWhiteSpace(), (ItemMasterGridRow r) => r.ItemCode.Contains(input.Filter) || r.ItemName.Contains(input.Filter) || r.Barcode.Contains(input.Filter)).WhereIf(input.ItemCategoryId.HasValue, (ItemMasterGridRow r) => r.ItemCategoryId == input.ItemCategoryId.Value)
				.WhereIf(input.ItemSubCategoryId.HasValue, (ItemMasterGridRow r) => r.ItemSubCategoryId == (int?)input.ItemSubCategoryId.Value)
				.WhereIf(input.IsActive.HasValue, (ItemMasterGridRow r) => r.IsActive == input.IsActive.Value);
		}

		private static IQueryable<ItemMasterGridRow> ApplyStatus(IQueryable<ItemMasterGridRow> query, ItemStatusFilter status)
		{
			switch (status)
			{
			case ItemStatusFilter.InStock:
				return query.Where((ItemMasterGridRow r) => r.StatusCode == 0);
			case ItemStatusFilter.NeedsReorder:
				return query.Where((ItemMasterGridRow r) => r.StatusCode == 1 || r.StatusCode == 2);
			default:
				return query;
			}
		}

		private async Task ValidateAsync(StockPilot.Entity.ItemMaster entity)
		{
			if (await _categoryRepository.FirstOrDefaultAsync(entity.ItemCategoryId) == null)
			{
				throw new UserFriendlyException(L("ItemCategoryNotFound"));
			}
			if (entity.ItemSubCategoryId.HasValue)
			{
				StockPilot.Entity.ItemSubCategory subCategory = await _subCategoryRepository.FirstOrDefaultAsync(entity.ItemSubCategoryId.Value);
				if (subCategory == null || subCategory.ItemCategoryId != entity.ItemCategoryId)
				{
					throw new UserFriendlyException(L("SubCategoryNotInCategory"));
				}
			}
			if (await _unitSetRepository.FirstOrDefaultAsync(entity.UnitSetId) == null)
			{
				throw new UserFriendlyException(L("UnitSetNotFound"));
			}
			StockPilot.Entity.UnitMaster baseUnit = await _unitRepository.FirstOrDefaultAsync(entity.BaseUnitId);
			if (baseUnit == null || baseUnit.UnitSetId != entity.UnitSetId)
			{
				throw new UserFriendlyException(L("BaseUnitNotInUnitSet", entity.ItemName));
			}
			if (entity.PurchaseUnitId.HasValue)
			{
				await _unitConversionManager.GetFactorAsync(entity, entity.PurchaseUnitId.Value);
			}
			if (entity.SalesUnitId.HasValue)
			{
				await _unitConversionManager.GetFactorAsync(entity, entity.SalesUnitId.Value);
			}
			bool flag = entity.DefaultSupplierId.HasValue;
			if (flag)
			{
				flag = await _supplierRepository.FirstOrDefaultAsync(entity.DefaultSupplierId.Value) == null;
			}
			if (flag)
			{
				throw new UserFriendlyException(L("SupplierNotFound"));
			}
			if (entity.MaxStock > 0m && entity.MaxStock < entity.MinStock)
			{
				throw new UserFriendlyException(L("MaxStockBelowMinStock"));
			}
		}

		private async Task<bool> HasMovementsAsync(int itemId)
		{
			return await _movementRepository.CountAsync((StockPilot.Entity.StockMovement m) => m.ItemMasterId == itemId) > 0;
		}

		private async Task EnsureCodeIsFreeAsync(string code, int? exceptId)
		{
			if (await _itemRepository.CountAsync((StockPilot.Entity.ItemMaster i) => i.ItemCode == code && (int?)i.Id != exceptId) > 0)
			{
				throw CodeExists(code);
			}
		}
	}
}
