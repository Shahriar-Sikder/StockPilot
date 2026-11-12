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
using StockPilot.Services.UnitMaster.Dtos;
using StockPilot.Services.UnitMaster.Exporting;

namespace StockPilot.Services.UnitMaster
{
	[AbpAuthorize(new string[] { })]
	public class UnitMasterAppService : MasterDataAppServiceBase, IUnitMasterAppService, IApplicationService, ITransientDependency
	{
		private static readonly Dictionary<string, string> SortFields = new Dictionary<string, string>
		{
			{ "code", "Code" },
			{ "name", "Name" },
			{ "unitSetName", "UnitSetInfo.Name" },
			{ "isBaseUnit", "IsBaseUnit" },
			{ "isActive", "IsActive" },
			{ "sequence", "Sequence" },
			{ "creationTime", "CreationTime" }
		};

		private readonly IRepository<StockPilot.Entity.UnitMaster> _unitRepository;

		private readonly IRepository<StockPilot.Entity.UnitSet> _unitSetRepository;

		private readonly IRepository<StockPilot.Entity.UnitConversion> _conversionRepository;

		private readonly IRepository<StockPilot.Entity.ItemMaster> _itemRepository;

		private readonly IRepository<PurchaseInvoiceItem> _purchaseLineRepository;

		private readonly IRepository<IssueSalesItem> _saleLineRepository;

		public UnitMasterExcelExporter ExcelExporter { get; set; }

		public UnitMasterAppService(IRepository<StockPilot.Entity.UnitMaster> unitRepository, IRepository<StockPilot.Entity.UnitSet> unitSetRepository, IRepository<StockPilot.Entity.UnitConversion> conversionRepository, IRepository<StockPilot.Entity.ItemMaster> itemRepository, IRepository<PurchaseInvoiceItem> purchaseLineRepository, IRepository<IssueSalesItem> saleLineRepository)
		{
			_unitRepository = unitRepository;
			_unitSetRepository = unitSetRepository;
			_conversionRepository = conversionRepository;
			_itemRepository = itemRepository;
			_purchaseLineRepository = purchaseLineRepository;
			_saleLineRepository = saleLineRepository;
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.Units" })]
		public async Task<PagedResultDto<UnitMasterListDto>> GetAll(UnitMasterGridInput input)
		{
			using (IncludeDeleted(input.IncludeDeleted))
			{
				IQueryable<StockPilot.Entity.UnitMaster> query = FilteredQuery(input);
				int totalCount = await query.CountAsync();
				List<StockPilot.Entity.UnitMaster> rows = await MasterDataAppServiceBase.ApplySorting(query, input.Sorting, "UnitSetInfo.Name ascending, IsBaseUnit descending, Sequence ascending, Code ascending", SortFields).PageBy(input).ToListAsync();
				return new PagedResultDto<UnitMasterListDto>(totalCount, base.ObjectMapper.Map<List<UnitMasterListDto>>(rows));
			}
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.Units" })]
		[RequiresFeature(new string[] { "App.Inventory.Export" })]
		public async Task<FileDto> GetAllToExcel(UnitMasterGridInput input)
		{
			using (IncludeDeleted(input.IncludeDeleted))
			{
				List<StockPilot.Entity.UnitMaster> rows = await MasterDataAppServiceBase.ApplySorting(FilteredQuery(input), input.Sorting, "UnitSetInfo.Name ascending, IsBaseUnit descending, Sequence ascending, Code ascending", SortFields).Take(100000).ToListAsync();
				return ExcelExporter.Export(base.ObjectMapper.Map<List<UnitMasterListDto>>(rows));
			}
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.Units" })]
		public async Task<EditUnitMasterDto> GetForEdit(EntityDto input)
		{
			IObjectMapper objectMapper = base.ObjectMapper;
			return objectMapper.Map<EditUnitMasterDto>(await _unitRepository.GetAsync(input.Id));
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.Units.Create" })]
		public async Task<EntityDto> Create(CreateUnitMasterDto input)
		{
			input.Code = input.Code.Trim();
			await EnsureUnitSetExistsAsync(input.UnitSetId);
			await EnsureCodeIsFreeAsync(input.Code, null);
			if (input.IsBaseUnit)
			{
				await EnsureNoOtherBaseUnitAsync(input.UnitSetId, null);
			}
			return new EntityDto(await _unitRepository.InsertAndGetIdAsync(MapForCreate<StockPilot.Entity.UnitMaster>(input)));
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.Units.Edit" })]
		public async Task Update(EditUnitMasterDto input)
		{
			input.Code = input.Code.Trim();
			StockPilot.Entity.UnitMaster entity = await _unitRepository.GetAsync(input.Id);
			await EnsureUnitSetExistsAsync(input.UnitSetId);
			await EnsureCodeIsFreeAsync(input.Code, entity.Id);
			if (input.IsBaseUnit)
			{
				await EnsureNoOtherBaseUnitAsync(input.UnitSetId, entity.Id);
			}
			if (input.UnitSetId != entity.UnitSetId)
			{
				string usedBy = await FindUsageAsync(entity.Id, includeDocuments: false);
				if (usedBy != null)
				{
					throw new UserFriendlyException(L("UnitSetCannotChangeInUse", entity.Code, usedBy));
				}
			}
			MapForUpdate(input, entity);
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.Units.Delete" })]
		public async Task Delete(EntityDto input)
		{
			StockPilot.Entity.UnitMaster entity = await _unitRepository.GetAsync(input.Id);
			string usedBy = await FindUsageAsync(entity.Id, includeDocuments: true);
			if (usedBy != null)
			{
				throw InUse(entity.Code, usedBy);
			}
			await _unitRepository.DeleteAsync(entity);
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.Units.Delete" })]
		public async Task Restore(EntityDto input)
		{
			await RestoreEntityAsync(_unitRepository, input.Id, async delegate(StockPilot.Entity.UnitMaster entity)
			{
				await EnsureUnitSetExistsAsync(entity.UnitSetId);
				await EnsureCodeIsFreeAsync(entity.Code, entity.Id);
				if (entity.IsBaseUnit)
				{
					await EnsureNoOtherBaseUnitAsync(entity.UnitSetId, entity.Id);
				}
			});
		}

		public async Task<ListResultDto<UnitMasterComboDto>> GetForCombo()
		{
			List<StockPilot.Entity.UnitMaster> rows = await (from u in _unitRepository.GetAll()
				where u.IsActive
				orderby u.UnitSetId, u.IsBaseUnit descending, u.Sequence, u.Code
				select u).ToListAsync();
			return new ListResultDto<UnitMasterComboDto>(base.ObjectMapper.Map<List<UnitMasterComboDto>>(rows));
		}

		private async Task EnsureUnitSetExistsAsync(int unitSetId)
		{
			if (await _unitSetRepository.FirstOrDefaultAsync(unitSetId) == null)
			{
				throw new UserFriendlyException(L("UnitSetNotFound"));
			}
		}

		private async Task EnsureCodeIsFreeAsync(string code, int? exceptId)
		{
			if (await _unitRepository.CountAsync((StockPilot.Entity.UnitMaster u) => u.Code == code && (int?)u.Id != exceptId) > 0)
			{
				throw CodeExists(code);
			}
		}

		private async Task EnsureNoOtherBaseUnitAsync(int unitSetId, int? exceptId)
		{
			StockPilot.Entity.UnitMaster existing = await _unitRepository.FirstOrDefaultAsync((StockPilot.Entity.UnitMaster u) => u.UnitSetId == unitSetId && u.IsBaseUnit && (int?)u.Id != exceptId);
			if (existing != null)
			{
				throw new UserFriendlyException(L("BaseUnitAlreadyExists", existing.Code));
			}
		}

		private async Task<string> FindUsageAsync(int unitId, bool includeDocuments)
		{
			if (await _itemRepository.CountAsync((StockPilot.Entity.ItemMaster i) => i.BaseUnitId == unitId || i.PurchaseUnitId == (int?)unitId || i.SalesUnitId == (int?)unitId) > 0)
			{
				return L("Items");
			}
			if (await _conversionRepository.CountAsync((StockPilot.Entity.UnitConversion c) => c.FromUnitId == unitId || c.ToUnitId == unitId) > 0)
			{
				return L("UnitConversions");
			}
			bool flag = includeDocuments;
			if (flag)
			{
				bool flag2 = await _purchaseLineRepository.CountAsync((PurchaseInvoiceItem l) => l.UnitMasterId == unitId) > 0;
				if (!flag2)
				{
					flag2 = await _saleLineRepository.CountAsync((IssueSalesItem l) => l.UnitMasterId == unitId) > 0;
				}
				flag = flag2;
			}
			if (flag)
			{
				return L("Documents");
			}
			return null;
		}

		private IQueryable<StockPilot.Entity.UnitMaster> FilteredQuery(UnitMasterGridInput input)
		{
			return _unitRepository.GetAll().Include((StockPilot.Entity.UnitMaster u) => u.UnitSetInfo).WhereIf(!input.Filter.IsNullOrWhiteSpace(), (StockPilot.Entity.UnitMaster u) => u.Code.Contains(input.Filter) || u.Name.Contains(input.Filter))
				.WhereIf(input.UnitSetId.HasValue, (StockPilot.Entity.UnitMaster u) => u.UnitSetId == input.UnitSetId.Value)
				.WhereIf(input.IsActive.HasValue, (StockPilot.Entity.UnitMaster u) => u.IsActive == input.IsActive.Value);
		}
	}
}
