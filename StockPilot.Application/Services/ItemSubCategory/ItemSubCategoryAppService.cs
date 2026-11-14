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
using Abp.UI;
using StockPilot.Dto;
using StockPilot.Entity;
using StockPilot.Services.Common;
using StockPilot.Services.Common.Dtos;
using StockPilot.Services.ItemSubCategory.Dtos;
using StockPilot.Services.ItemSubCategory.Exporting;

namespace StockPilot.Services.ItemSubCategory
{
	[AbpAuthorize(new string[] { })]
	public class ItemSubCategoryAppService : MasterDataAppServiceBase, IItemSubCategoryAppService, IApplicationService, ITransientDependency
	{
		private static readonly Dictionary<string, string> SortFields = new Dictionary<string, string>
		{
			{ "code", "Code" },
			{ "name", "Name" },
			{ "itemCategoryName", "ItemCategoryInfo.Name" },
			{ "isActive", "IsActive" },
			{ "sequence", "Sequence" },
			{ "creationTime", "CreationTime" }
		};

		private readonly IRepository<StockPilot.Entity.ItemSubCategory> _subCategoryRepository;

		private readonly IRepository<StockPilot.Entity.ItemCategory> _categoryRepository;

		private readonly IRepository<StockPilot.Entity.ItemMaster> _itemRepository;

		private readonly EntityPictureFiles _pictureFiles;

		public ItemSubCategoryExcelExporter ExcelExporter { get; set; }

		public ItemSubCategoryAppService(IRepository<StockPilot.Entity.ItemSubCategory> subCategoryRepository, IRepository<StockPilot.Entity.ItemCategory> categoryRepository, IRepository<StockPilot.Entity.ItemMaster> itemRepository, EntityPictureFiles pictureFiles)
		{
			_subCategoryRepository = subCategoryRepository;
			_categoryRepository = categoryRepository;
			_itemRepository = itemRepository;
			_pictureFiles = pictureFiles;
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.SubCategories" })]
		public async Task<PagedResultDto<ItemSubCategoryListDto>> GetAll(ItemSubCategoryGridInput input)
		{
			using (IncludeDeleted(input.IncludeDeleted))
			{
				IQueryable<StockPilot.Entity.ItemSubCategory> query = FilteredQuery(input);
				int totalCount = await query.CountAsync();
				List<StockPilot.Entity.ItemSubCategory> rows = await MasterDataAppServiceBase.ApplySorting(query, input.Sorting, "ItemCategoryInfo.Name ascending, Sequence ascending, Name ascending", SortFields).PageBy(input).ToListAsync();
				return new PagedResultDto<ItemSubCategoryListDto>(totalCount, base.ObjectMapper.Map<List<ItemSubCategoryListDto>>(rows));
			}
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.SubCategories" })]
		[RequiresFeature(new string[] { "App.Inventory.Export" })]
		public async Task<FileDto> GetAllToExcel(ItemSubCategoryGridInput input)
		{
			using (IncludeDeleted(input.IncludeDeleted))
			{
				List<StockPilot.Entity.ItemSubCategory> rows = await MasterDataAppServiceBase.ApplySorting(FilteredQuery(input), input.Sorting, "ItemCategoryInfo.Name ascending, Sequence ascending, Name ascending", SortFields).Take(100000).ToListAsync();
				return ExcelExporter.Export(base.ObjectMapper.Map<List<ItemSubCategoryListDto>>(rows));
			}
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.SubCategories" })]
		public async Task<EditItemSubCategoryDto> GetForEdit(EntityDto input)
		{
			IObjectMapper objectMapper = base.ObjectMapper;
			return objectMapper.Map<EditItemSubCategoryDto>(await _subCategoryRepository.GetAsync(input.Id));
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.SubCategories.Create" })]
		public async Task<EntityDto> Create(CreateItemSubCategoryDto input)
		{
			input.Code = input.Code.Trim();
			await EnsureCategoryExistsAsync(input.ItemCategoryId);
			await EnsureCodeIsFreeAsync(input.Code, null);
			StockPilot.Entity.ItemSubCategory entity = MapForCreate<StockPilot.Entity.ItemSubCategory>(input);
			entity.PictureId = null;
			entity.PictureUrl = null;
			return new EntityDto(await _subCategoryRepository.InsertAndGetIdAsync(entity));
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.SubCategories.Edit" })]
		public async Task Update(EditItemSubCategoryDto input)
		{
			input.Code = input.Code.Trim();
			StockPilot.Entity.ItemSubCategory entity = await _subCategoryRepository.GetAsync(input.Id);
			await EnsureCategoryExistsAsync(input.ItemCategoryId);
			await EnsureCodeIsFreeAsync(input.Code, entity.Id);
			bool flag = input.ItemCategoryId != entity.ItemCategoryId;
			if (flag)
			{
				flag = await _itemRepository.CountAsync((StockPilot.Entity.ItemMaster i) => i.ItemSubCategoryId == (int?)entity.Id) > 0;
			}
			if (flag)
			{
				throw new UserFriendlyException(L("SubCategoryCannotChangeCategoryInUse", entity.Name));
			}
			Guid? pictureId = entity.PictureId;
			string pictureUrl = entity.PictureUrl;
			MapForUpdate(input, entity);
			entity.PictureId = pictureId;
			entity.PictureUrl = pictureUrl;
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.SubCategories.Delete" })]
		public async Task Delete(EntityDto input)
		{
			StockPilot.Entity.ItemSubCategory entity = await _subCategoryRepository.GetAsync(input.Id);
			if (await _itemRepository.CountAsync((StockPilot.Entity.ItemMaster i) => i.ItemSubCategoryId == (int?)entity.Id) > 0)
			{
				throw InUse(entity.Name, L("Items"));
			}
			await _subCategoryRepository.DeleteAsync(entity);
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.SubCategories.Delete" })]
		public async Task Restore(EntityDto input)
		{
			await RestoreEntityAsync(_subCategoryRepository, input.Id, async delegate(StockPilot.Entity.ItemSubCategory entity)
			{
				await EnsureCategoryExistsAsync(entity.ItemCategoryId);
				await EnsureCodeIsFreeAsync(entity.Code, entity.Id);
			});
		}

		public async Task<ListResultDto<ItemSubCategoryComboDto>> GetForCombo()
		{
			List<StockPilot.Entity.ItemSubCategory> rows = await (from s in _subCategoryRepository.GetAll()
				where s.IsActive
				orderby s.ItemCategoryId, s.Sequence, s.Name
				select s).ToListAsync();
			return new ListResultDto<ItemSubCategoryComboDto>(base.ObjectMapper.Map<List<ItemSubCategoryComboDto>>(rows));
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.SubCategories.Edit" })]
		[RequiresFeature(new string[] { "App.Inventory.Pictures" })]
		public async Task<PictureUrlDto> UploadPicture(UploadPictureInput input)
		{
			StockPilot.Entity.ItemSubCategory entity = await _subCategoryRepository.GetAsync(input.Id);
			// The new file now; the previous one is deleted after the save (EntityPictureFiles).
			entity.PictureUrl = await _pictureFiles.ReplaceAsync(entity.TenantId, PictureFolders.SubCategories, input.Base64Content, entity.PictureUrl, entity.PictureId);
			entity.PictureId = null;
			return new PictureUrlDto { PictureUrl = entity.PictureUrl };
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.SubCategories.Edit" })]
		[RequiresFeature(new string[] { "App.Inventory.Pictures" })]
		public async Task DeletePicture(EntityDto input)
		{
			StockPilot.Entity.ItemSubCategory entity = await _subCategoryRepository.GetAsync(input.Id);
			_pictureFiles.RemoveAfterCommit(entity.PictureUrl, entity.PictureId);
			entity.PictureUrl = null;
			entity.PictureId = null;
		}

		private async Task EnsureCategoryExistsAsync(int categoryId)
		{
			if (await _categoryRepository.FirstOrDefaultAsync(categoryId) == null)
			{
				throw new UserFriendlyException(L("ItemCategoryNotFound"));
			}
		}

		private async Task EnsureCodeIsFreeAsync(string code, int? exceptId)
		{
			if (await _subCategoryRepository.CountAsync((StockPilot.Entity.ItemSubCategory s) => s.Code == code && (int?)s.Id != exceptId) > 0)
			{
				throw CodeExists(code);
			}
		}

		private IQueryable<StockPilot.Entity.ItemSubCategory> FilteredQuery(ItemSubCategoryGridInput input)
		{
			return _subCategoryRepository.GetAll().Include((StockPilot.Entity.ItemSubCategory s) => s.ItemCategoryInfo).WhereIf(!input.Filter.IsNullOrWhiteSpace(), (StockPilot.Entity.ItemSubCategory s) => s.Code.Contains(input.Filter) || s.Name.Contains(input.Filter) || s.Description.Contains(input.Filter))
				.WhereIf(input.ItemCategoryId.HasValue, (StockPilot.Entity.ItemSubCategory s) => s.ItemCategoryId == input.ItemCategoryId.Value)
				.WhereIf(input.IsActive.HasValue, (StockPilot.Entity.ItemSubCategory s) => s.IsActive == input.IsActive.Value);
		}
	}
}
