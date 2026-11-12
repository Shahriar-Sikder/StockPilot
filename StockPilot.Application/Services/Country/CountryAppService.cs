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
using StockPilot.Services.Country.Dtos;
using StockPilot.Services.Country.Exporting;

namespace StockPilot.Services.Country
{
	[AbpAuthorize(new string[] { })]
	public class CountryAppService : MasterDataAppServiceBase, ICountryAppService, IApplicationService, ITransientDependency
	{
		private static readonly Dictionary<string, string> SortFields = new Dictionary<string, string>
		{
			{ "code", "Code" },
			{ "name", "Name" },
			{ "phoneCode", "PhoneCode" },
			{ "isActive", "IsActive" },
			{ "sequence", "Sequence" },
			{ "creationTime", "CreationTime" }
		};

		private readonly IRepository<StockPilot.Entity.Country> _countryRepository;

		private readonly IRepository<StockPilot.Entity.Supplier> _supplierRepository;

		public CountryExcelExporter ExcelExporter { get; set; }

		public CountryAppService(IRepository<StockPilot.Entity.Country> countryRepository, IRepository<StockPilot.Entity.Supplier> supplierRepository)
		{
			_countryRepository = countryRepository;
			_supplierRepository = supplierRepository;
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.Countries" })]
		public async Task<PagedResultDto<CountryListDto>> GetAll(CountryGridInput input)
		{
			using (IncludeDeleted(input.IncludeDeleted))
			{
				IQueryable<StockPilot.Entity.Country> query = FilteredQuery(input);
				int totalCount = await query.CountAsync();
				List<StockPilot.Entity.Country> rows = await MasterDataAppServiceBase.ApplySorting(query, input.Sorting, "Sequence ascending, Name ascending", SortFields).PageBy(input).ToListAsync();
				return new PagedResultDto<CountryListDto>(totalCount, base.ObjectMapper.Map<List<CountryListDto>>(rows));
			}
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.Countries" })]
		[RequiresFeature(new string[] { "App.Inventory.Export" })]
		public async Task<FileDto> GetAllToExcel(CountryGridInput input)
		{
			using (IncludeDeleted(input.IncludeDeleted))
			{
				List<StockPilot.Entity.Country> rows = await MasterDataAppServiceBase.ApplySorting(FilteredQuery(input), input.Sorting, "Sequence ascending, Name ascending", SortFields).Take(100000).ToListAsync();
				return ExcelExporter.Export(base.ObjectMapper.Map<List<CountryListDto>>(rows));
			}
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.Countries" })]
		public async Task<EditCountryDto> GetForEdit(EntityDto input)
		{
			IObjectMapper objectMapper = base.ObjectMapper;
			return objectMapper.Map<EditCountryDto>(await _countryRepository.GetAsync(input.Id));
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.Countries.Create" })]
		public async Task<EntityDto> Create(CreateCountryDto input)
		{
			input.Code = input.Code.Trim();
			await EnsureCodeIsFreeAsync(input.Code, null);
			return new EntityDto(await _countryRepository.InsertAndGetIdAsync(MapForCreate<StockPilot.Entity.Country>(input)));
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.Countries.Edit" })]
		public async Task Update(EditCountryDto input)
		{
			input.Code = input.Code.Trim();
			StockPilot.Entity.Country entity = await _countryRepository.GetAsync(input.Id);
			await EnsureCodeIsFreeAsync(input.Code, entity.Id);
			MapForUpdate(input, entity);
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.Countries.Delete" })]
		public async Task Delete(EntityDto input)
		{
			StockPilot.Entity.Country entity = await _countryRepository.GetAsync(input.Id);
			if (await _supplierRepository.CountAsync((StockPilot.Entity.Supplier s) => s.CountryId == (int?)entity.Id) > 0)
			{
				throw InUse(entity.Name, L("Suppliers"));
			}
			await _countryRepository.DeleteAsync(entity);
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Setup.Countries.Delete" })]
		public async Task Restore(EntityDto input)
		{
			await RestoreEntityAsync(_countryRepository, input.Id, (StockPilot.Entity.Country entity) => EnsureCodeIsFreeAsync(entity.Code, entity.Id));
		}

		public async Task<ListResultDto<CountryComboDto>> GetForCombo()
		{
			List<StockPilot.Entity.Country> rows = await (from c in _countryRepository.GetAll()
				where c.IsActive
				orderby c.Sequence, c.Name
				select c).ToListAsync();
			return new ListResultDto<CountryComboDto>(base.ObjectMapper.Map<List<CountryComboDto>>(rows));
		}

		private async Task EnsureCodeIsFreeAsync(string code, int? exceptId)
		{
			if (await _countryRepository.CountAsync((StockPilot.Entity.Country c) => c.Code == code && (int?)c.Id != exceptId) > 0)
			{
				throw CodeExists(code);
			}
		}

		private IQueryable<StockPilot.Entity.Country> FilteredQuery(CountryGridInput input)
		{
			return _countryRepository.GetAll().WhereIf(!input.Filter.IsNullOrWhiteSpace(), (StockPilot.Entity.Country c) => c.Code.Contains(input.Filter) || c.Name.Contains(input.Filter) || c.PhoneCode.Contains(input.Filter)).WhereIf(input.IsActive.HasValue, (StockPilot.Entity.Country c) => c.IsActive == input.IsActive.Value);
		}
	}
}
