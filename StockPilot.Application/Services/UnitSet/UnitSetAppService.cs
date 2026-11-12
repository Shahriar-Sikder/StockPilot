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
using StockPilot.Services.UnitSet.Dtos;
using StockPilot.Services.UnitSet.Exporting;

namespace StockPilot.Services.UnitSet
{
	[AbpAuthorize(new string[] { })]
	public class UnitSetAppService : MasterDataAppServiceBase, IUnitSetAppService, IApplicationService, ITransientDependency
	{
		private static readonly Dictionary<string, string> SortFields = new Dictionary<string, string>
		{
			{ "code", "Code" },
			{ "name", "Name" },
			{ "isActive", "IsActive" },
			{ "creationTime", "CreationTime" }
		};

		private readonly IRepository<StockPilot.Entity.UnitSet> _unitSetRepository;

		private readonly IRepository<StockPilot.Entity.UnitMaster> _unitRepository;

		private readonly IRepository<StockPilot.Entity.ItemMaster> _itemRepository;

		public UnitSetExcelExporter ExcelExporter { get; set; }

		public UnitSetAppService(IRepository<StockPilot.Entity.UnitSet> unitSetRepository, IRepository<StockPilot.Entity.UnitMaster> unitRepository, IRepository<StockPilot.Entity.ItemMaster> itemRepository)
		{
			_unitSetRepository = unitSetRepository;
			_unitRepository = unitRepository;
			_itemRepository = itemRepository;
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.UnitSets" })]
		public async Task<PagedResultDto<UnitSetListDto>> GetAll(UnitSetGridInput input)
		{
			using (IncludeDeleted(input.IncludeDeleted))
			{
				IQueryable<StockPilot.Entity.UnitSet> query = FilteredQuery(input);
				int totalCount = await query.CountAsync();
				List<StockPilot.Entity.UnitSet> rows = await MasterDataAppServiceBase.ApplySorting(query, input.Sorting, "Name ascending", SortFields).PageBy(input).ToListAsync();
				return new PagedResultDto<UnitSetListDto>(totalCount, base.ObjectMapper.Map<List<UnitSetListDto>>(rows));
			}
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.UnitSets" })]
		[RequiresFeature(new string[] { "App.Inventory.Export" })]
		public async Task<FileDto> GetAllToExcel(UnitSetGridInput input)
		{
			using (IncludeDeleted(input.IncludeDeleted))
			{
				List<StockPilot.Entity.UnitSet> rows = await MasterDataAppServiceBase.ApplySorting(FilteredQuery(input), input.Sorting, "Name ascending", SortFields).Take(100000).ToListAsync();
				return ExcelExporter.Export(base.ObjectMapper.Map<List<UnitSetListDto>>(rows));
			}
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.UnitSets" })]
		public async Task<EditUnitSetDto> GetForEdit(EntityDto input)
		{
			IObjectMapper objectMapper = base.ObjectMapper;
			return objectMapper.Map<EditUnitSetDto>(await _unitSetRepository.GetAsync(input.Id));
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.UnitSets.Create" })]
		public async Task<EntityDto> Create(CreateUnitSetDto input)
		{
			input.Code = input.Code.Trim();
			await EnsureCodeIsFreeAsync(input.Code, null);
			return new EntityDto(await _unitSetRepository.InsertAndGetIdAsync(MapForCreate<StockPilot.Entity.UnitSet>(input)));
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.UnitSets.Edit" })]
		public async Task Update(EditUnitSetDto input)
		{
			input.Code = input.Code.Trim();
			StockPilot.Entity.UnitSet entity = await _unitSetRepository.GetAsync(input.Id);
			await EnsureCodeIsFreeAsync(input.Code, entity.Id);
			MapForUpdate(input, entity);
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.UnitSets.Delete" })]
		public async Task Delete(EntityDto input)
		{
			StockPilot.Entity.UnitSet entity = await _unitSetRepository.GetAsync(input.Id);
			if (await _unitRepository.CountAsync((StockPilot.Entity.UnitMaster u) => u.UnitSetId == entity.Id) > 0)
			{
				throw InUse(entity.Name, L("Units"));
			}
			if (await _itemRepository.CountAsync((StockPilot.Entity.ItemMaster i) => i.UnitSetId == entity.Id) > 0)
			{
				throw InUse(entity.Name, L("Items"));
			}
			await _unitSetRepository.DeleteAsync(entity);
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.UnitSets.Delete" })]
		public async Task Restore(EntityDto input)
		{
			await RestoreEntityAsync(_unitSetRepository, input.Id, (StockPilot.Entity.UnitSet entity) => EnsureCodeIsFreeAsync(entity.Code, entity.Id));
		}

		public async Task<ListResultDto<UnitSetComboDto>> GetForCombo()
		{
			List<StockPilot.Entity.UnitSet> rows = await (from s in _unitSetRepository.GetAll()
				where s.IsActive
				orderby s.Name
				select s).ToListAsync();
			return new ListResultDto<UnitSetComboDto>(base.ObjectMapper.Map<List<UnitSetComboDto>>(rows));
		}

		private async Task EnsureCodeIsFreeAsync(string code, int? exceptId)
		{
			if (await _unitSetRepository.CountAsync((StockPilot.Entity.UnitSet s) => s.Code == code && (int?)s.Id != exceptId) > 0)
			{
				throw CodeExists(code);
			}
		}

		private IQueryable<StockPilot.Entity.UnitSet> FilteredQuery(UnitSetGridInput input)
		{
			return _unitSetRepository.GetAll().WhereIf(!input.Filter.IsNullOrWhiteSpace(), (StockPilot.Entity.UnitSet s) => s.Code.Contains(input.Filter) || s.Name.Contains(input.Filter) || s.Description.Contains(input.Filter)).WhereIf(input.IsActive.HasValue, (StockPilot.Entity.UnitSet s) => s.IsActive == input.IsActive.Value);
		}
	}
}
