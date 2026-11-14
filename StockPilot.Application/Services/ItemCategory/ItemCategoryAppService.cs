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
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.ObjectMapping;
using StockPilot.Dto;
using StockPilot.Entity;
using StockPilot.Services.Common;
using StockPilot.Services.Common.Dtos;
using StockPilot.Services.ItemCategory.Dtos;
using StockPilot.Services.ItemCategory.Exporting;

namespace StockPilot.Services.ItemCategory
{
	[AbpAuthorize(new string[] { })]
	public class ItemCategoryAppService : MasterDataAppServiceBase, IItemCategoryAppService, IApplicationService, ITransientDependency
	{
		private static readonly Dictionary<string, string> SortFields = new Dictionary<string, string>
		{
			{ "code", "Code" },
			{ "name", "Name" },
			{ "isActive", "IsActive" },
			{ "sequence", "Sequence" },
			{ "creationTime", "CreationTime" }
		};

		private readonly IRepository<StockPilot.Entity.ItemCategory> _categoryRepository;

		private readonly IRepository<StockPilot.Entity.ItemSubCategory> _subCategoryRepository;

		private readonly IRepository<StockPilot.Entity.ItemMaster> _itemRepository;

		private readonly EntityPictureFiles _pictureFiles;

		public ItemCategoryExcelExporter ExcelExporter { get; set; }

		public ItemCategoryAppService(IRepository<StockPilot.Entity.ItemCategory> categoryRepository, IRepository<StockPilot.Entity.ItemSubCategory> subCategoryRepository, IRepository<StockPilot.Entity.ItemMaster> itemRepository, EntityPictureFiles pictureFiles)
		{
			_categoryRepository = categoryRepository;
			_subCategoryRepository = subCategoryRepository;
			_itemRepository = itemRepository;
			_pictureFiles = pictureFiles;
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.Categories" })]
		public async Task<PagedResultDto<ItemCategoryListDto>> GetAll(ItemCategoryGridInput input)
		{
			using (IncludeDeleted(input.IncludeDeleted))
			{
				IQueryable<StockPilot.Entity.ItemCategory> query = FilteredQuery(input);
				int totalCount = await query.CountAsync();
				List<StockPilot.Entity.ItemCategory> rows = await MasterDataAppServiceBase.ApplySorting(query, input.Sorting, "Sequence ascending, Name ascending", SortFields).PageBy(input).ToListAsync();
				return new PagedResultDto<ItemCategoryListDto>(totalCount, base.ObjectMapper.Map<List<ItemCategoryListDto>>(rows));
			}
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.Categories" })]
		[RequiresFeature(new string[] { "App.Inventory.Export" })]
		public async Task<FileDto> GetAllToExcel(ItemCategoryGridInput input)
		{
			using (IncludeDeleted(input.IncludeDeleted))
			{
				List<StockPilot.Entity.ItemCategory> rows = await MasterDataAppServiceBase.ApplySorting(FilteredQuery(input), input.Sorting, "Sequence ascending, Name ascending", SortFields).Take(100000).ToListAsync();
				return ExcelExporter.Export(base.ObjectMapper.Map<List<ItemCategoryListDto>>(rows));
			}
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.Categories" })]
		public async Task<EditItemCategoryDto> GetForEdit(EntityDto input)
		{
			IObjectMapper objectMapper = base.ObjectMapper;
			return objectMapper.Map<EditItemCategoryDto>(await _categoryRepository.GetAsync(input.Id));
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.Categories.Create" })]
		public async Task<EntityDto> Create(CreateItemCategoryDto input)
		{
			input.Code = input.Code.Trim();
			await EnsureCodeIsFreeAsync(input.Code, null);
			StockPilot.Entity.ItemCategory entity = MapForCreate<StockPilot.Entity.ItemCategory>(input);
			entity.PictureId = null;
			entity.PictureUrl = null;
			return new EntityDto(await _categoryRepository.InsertAndGetIdAsync(entity));
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.Categories.Edit" })]
		public async Task Update(EditItemCategoryDto input)
		{
			input.Code = input.Code.Trim();
			StockPilot.Entity.ItemCategory entity = await _categoryRepository.GetAsync(input.Id);
			await EnsureCodeIsFreeAsync(input.Code, entity.Id);
			Guid? pictureId = entity.PictureId;
			string pictureUrl = entity.PictureUrl;
			MapForUpdate(input, entity);
			entity.PictureId = pictureId;
			entity.PictureUrl = pictureUrl;
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.Categories.Delete" })]
		public async Task Delete(EntityDto input)
		{
			StockPilot.Entity.ItemCategory entity = await _categoryRepository.GetAsync(input.Id);
			if (await _subCategoryRepository.CountAsync((StockPilot.Entity.ItemSubCategory s) => s.ItemCategoryId == entity.Id) > 0)
			{
				throw InUse(entity.Name, L("ItemSubCategories"));
			}
			if (await _itemRepository.CountAsync((StockPilot.Entity.ItemMaster i) => i.ItemCategoryId == entity.Id) > 0)
			{
				throw InUse(entity.Name, L("Items"));
			}
			await _categoryRepository.DeleteAsync(entity);
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.Categories.Delete" })]
		public async Task Restore(EntityDto input)
		{
			await RestoreEntityAsync(_categoryRepository, input.Id, (StockPilot.Entity.ItemCategory entity) => EnsureCodeIsFreeAsync(entity.Code, entity.Id));
		}

		public async Task<ListResultDto<ItemCategoryComboDto>> GetForCombo()
		{
			List<StockPilot.Entity.ItemCategory> rows = await (from c in _categoryRepository.GetAll()
				where c.IsActive
				orderby c.Sequence, c.Name
				select c).ToListAsync();
			return new ListResultDto<ItemCategoryComboDto>(base.ObjectMapper.Map<List<ItemCategoryComboDto>>(rows));
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.Categories.Edit" })]
		[RequiresFeature(new string[] { "App.Inventory.Pictures" })]
		public async Task<PictureUrlDto> UploadPicture(UploadPictureInput input)
		{
			StockPilot.Entity.ItemCategory entity = await _categoryRepository.GetAsync(input.Id);
			// The new file now; the previous one is deleted after the save (EntityPictureFiles).
			entity.PictureUrl = await _pictureFiles.ReplaceAsync(entity.TenantId, PictureFolders.Categories, input.Base64Content, entity.PictureUrl, entity.PictureId);
			entity.PictureId = null;
			return new PictureUrlDto { PictureUrl = entity.PictureUrl };
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.Categories.Edit" })]
		[RequiresFeature(new string[] { "App.Inventory.Pictures" })]
		public async Task DeletePicture(EntityDto input)
		{
			StockPilot.Entity.ItemCategory entity = await _categoryRepository.GetAsync(input.Id);
			_pictureFiles.RemoveAfterCommit(entity.PictureUrl, entity.PictureId);
			entity.PictureUrl = null;
			entity.PictureId = null;
		}

		private async Task EnsureCodeIsFreeAsync(string code, int? exceptId)
		{
			if (await _categoryRepository.CountAsync((StockPilot.Entity.ItemCategory c) => c.Code == code && (int?)c.Id != exceptId) > 0)
			{
				throw CodeExists(code);
			}
		}

		private IQueryable<StockPilot.Entity.ItemCategory> FilteredQuery(ItemCategoryGridInput input)
		{
			return _categoryRepository.GetAll().WhereIf(!input.Filter.IsNullOrWhiteSpace(), (StockPilot.Entity.ItemCategory c) => c.Code.Contains(input.Filter) || c.Name.Contains(input.Filter) || c.Description.Contains(input.Filter)).WhereIf(input.IsActive.HasValue, (StockPilot.Entity.ItemCategory c) => c.IsActive == input.IsActive.Value);
		}
	}
}
