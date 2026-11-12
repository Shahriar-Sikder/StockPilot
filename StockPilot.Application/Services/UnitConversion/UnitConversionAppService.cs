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
using StockPilot.Services.UnitConversion.Dtos;
using StockPilot.Services.UnitConversion.Exporting;

namespace StockPilot.Services.UnitConversion
{
	[AbpAuthorize(new string[] { })]
	public class UnitConversionAppService : MasterDataAppServiceBase, IUnitConversionAppService, IApplicationService, ITransientDependency
	{
		private static readonly Dictionary<string, string> SortFields = new Dictionary<string, string>
		{
			{ "fromUnitCode", "FromUnit.Code" },
			{ "toUnitCode", "ToUnit.Code" },
			{ "factor", "Factor" },
			{ "unitSetName", "UnitSetInfo.Name" },
			{ "isActive", "IsActive" },
			{ "creationTime", "CreationTime" }
		};

		private readonly IRepository<StockPilot.Entity.UnitConversion> _conversionRepository;

		private readonly IRepository<StockPilot.Entity.UnitMaster> _unitRepository;

		public UnitConversionExcelExporter ExcelExporter { get; set; }

		public UnitConversionAppService(IRepository<StockPilot.Entity.UnitConversion> conversionRepository, IRepository<StockPilot.Entity.UnitMaster> unitRepository)
		{
			_conversionRepository = conversionRepository;
			_unitRepository = unitRepository;
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.UnitConversions" })]
		public async Task<PagedResultDto<UnitConversionListDto>> GetAll(UnitConversionGridInput input)
		{
			using (IncludeDeleted(input.IncludeDeleted))
			{
				IQueryable<StockPilot.Entity.UnitConversion> query = FilteredQuery(input);
				int totalCount = await query.CountAsync();
				List<StockPilot.Entity.UnitConversion> rows = await MasterDataAppServiceBase.ApplySorting(query, input.Sorting, "UnitSetInfo.Name ascending, FromUnit.Code ascending", SortFields).PageBy(input).ToListAsync();
				return new PagedResultDto<UnitConversionListDto>(totalCount, base.ObjectMapper.Map<List<UnitConversionListDto>>(rows));
			}
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.UnitConversions" })]
		[RequiresFeature(new string[] { "App.Inventory.Export" })]
		public async Task<FileDto> GetAllToExcel(UnitConversionGridInput input)
		{
			using (IncludeDeleted(input.IncludeDeleted))
			{
				List<StockPilot.Entity.UnitConversion> rows = await MasterDataAppServiceBase.ApplySorting(FilteredQuery(input), input.Sorting, "UnitSetInfo.Name ascending, FromUnit.Code ascending", SortFields).Take(100000).ToListAsync();
				return ExcelExporter.Export(base.ObjectMapper.Map<List<UnitConversionListDto>>(rows));
			}
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.UnitConversions" })]
		public async Task<EditUnitConversionDto> GetForEdit(EntityDto input)
		{
			IObjectMapper objectMapper = base.ObjectMapper;
			return objectMapper.Map<EditUnitConversionDto>(await _conversionRepository.GetAsync(input.Id));
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.UnitConversions.Create" })]
		[RequiresFeature(new string[] { "App.Inventory.UnitConversions" })]
		public async Task<EntityDto> Create(CreateUnitConversionDto input)
		{
			input.UnitSetId = await ValidateAsync(input.FromUnitId, input.ToUnitId, input.Factor, null);
			return new EntityDto(await _conversionRepository.InsertAndGetIdAsync(MapForCreate<StockPilot.Entity.UnitConversion>(input)));
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.UnitConversions.Edit" })]
		[RequiresFeature(new string[] { "App.Inventory.UnitConversions" })]
		public async Task Update(EditUnitConversionDto input)
		{
			StockPilot.Entity.UnitConversion entity = await _conversionRepository.GetAsync(input.Id);
			input.UnitSetId = await ValidateAsync(input.FromUnitId, input.ToUnitId, input.Factor, entity.Id);
			MapForUpdate(input, entity);
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.UnitConversions.Delete" })]
		[RequiresFeature(new string[] { "App.Inventory.UnitConversions" })]
		public async Task Delete(EntityDto input)
		{
			await _conversionRepository.DeleteAsync(input.Id);
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.UnitConversions.Delete" })]
		[RequiresFeature(new string[] { "App.Inventory.UnitConversions" })]
		public async Task Restore(EntityDto input)
		{
			await RestoreEntityAsync(_conversionRepository, input.Id, (StockPilot.Entity.UnitConversion entity) => ValidateAsync(entity.FromUnitId, entity.ToUnitId, entity.Factor, entity.Id));
		}

		public async Task<ListResultDto<UnitConversionComboDto>> GetForCombo()
		{
			List<StockPilot.Entity.UnitConversion> rows = await (from c in _conversionRepository.GetAll().Include((StockPilot.Entity.UnitConversion c) => c.FromUnit).Include((StockPilot.Entity.UnitConversion c) => c.ToUnit)
				where c.IsActive
				orderby c.UnitSetId, c.FromUnit.Code
				select c).ToListAsync();
			return new ListResultDto<UnitConversionComboDto>(base.ObjectMapper.Map<List<UnitConversionComboDto>>(rows));
		}

		private async Task<int> ValidateAsync(int fromUnitId, int toUnitId, decimal factor, int? exceptId)
		{
			if (fromUnitId == toUnitId)
			{
				throw new UserFriendlyException(L("UnitConversionSameUnit"));
			}
			if (factor <= 0m)
			{
				throw new UserFriendlyException(L("FactorMustBePositive"));
			}
			StockPilot.Entity.UnitMaster fromUnit = await _unitRepository.FirstOrDefaultAsync(fromUnitId);
			StockPilot.Entity.UnitMaster toUnit = await _unitRepository.FirstOrDefaultAsync(toUnitId);
			if (fromUnit == null || toUnit == null)
			{
				throw new UserFriendlyException(L("UnitNotFound"));
			}
			if (fromUnit.UnitSetId != toUnit.UnitSetId)
			{
				throw new UserFriendlyException(L("UnitConversionDifferentSets", fromUnit.Code, toUnit.Code));
			}
			if (await _conversionRepository.CountAsync((StockPilot.Entity.UnitConversion c) => (int?)c.Id != exceptId && ((c.FromUnitId == fromUnitId && c.ToUnitId == toUnitId) || (c.FromUnitId == toUnitId && c.ToUnitId == fromUnitId))) > 0)
			{
				throw new UserFriendlyException(L("UnitConversionAlreadyExists", fromUnit.Code, toUnit.Code));
			}
			return fromUnit.UnitSetId;
		}

		private IQueryable<StockPilot.Entity.UnitConversion> FilteredQuery(UnitConversionGridInput input)
		{
			return _conversionRepository.GetAll().Include((StockPilot.Entity.UnitConversion c) => c.FromUnit).Include((StockPilot.Entity.UnitConversion c) => c.ToUnit)
				.Include((StockPilot.Entity.UnitConversion c) => c.UnitSetInfo)
				.WhereIf(!input.Filter.IsNullOrWhiteSpace(), (StockPilot.Entity.UnitConversion c) => c.FromUnit.Code.Contains(input.Filter) || c.ToUnit.Code.Contains(input.Filter))
				.WhereIf(input.UnitSetId.HasValue, (StockPilot.Entity.UnitConversion c) => c.UnitSetId == input.UnitSetId.Value)
				.WhereIf(input.FromUnitId.HasValue, (StockPilot.Entity.UnitConversion c) => c.FromUnitId == input.FromUnitId.Value)
				.WhereIf(input.ToUnitId.HasValue, (StockPilot.Entity.UnitConversion c) => c.ToUnitId == input.ToUnitId.Value)
				.WhereIf(input.IsActive.HasValue, (StockPilot.Entity.UnitConversion c) => c.IsActive == input.IsActive.Value);
		}
	}
}
