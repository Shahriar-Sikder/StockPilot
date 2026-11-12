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
using StockPilot.Services.Supplier.Dtos;
using StockPilot.Services.Supplier.Exporting;

namespace StockPilot.Services.Supplier
{
	[AbpAuthorize(new string[] { })]
	public class SupplierAppService : MasterDataAppServiceBase, ISupplierAppService, IApplicationService, ITransientDependency
	{
		private static readonly Dictionary<string, string> SortFields = new Dictionary<string, string>
		{
			{ "code", "Code" },
			{ "name", "Name" },
			{ "contactPerson", "ContactPerson" },
			{ "email", "Email" },
			{ "phone", "Phone" },
			{ "city", "City" },
			{ "countryName", "CountryInfo.Name" },
			{ "currencyCode", "CurrencyInfo.Code" },
			{ "paymentTermDays", "PaymentTermDays" },
			{ "isActive", "IsActive" },
			{ "sequence", "Sequence" },
			{ "creationTime", "CreationTime" }
		};

		private readonly IRepository<StockPilot.Entity.Supplier> _supplierRepository;

		private readonly IRepository<StockPilot.Entity.Country> _countryRepository;

		private readonly IRepository<StockPilot.Entity.Currency> _currencyRepository;

		private readonly IRepository<StockPilot.Entity.PurchaseInvoice> _purchaseInvoiceRepository;

		private readonly IRepository<StockPilot.Entity.ItemMaster> _itemRepository;

		public SupplierExcelExporter ExcelExporter { get; set; }

		public SupplierAppService(IRepository<StockPilot.Entity.Supplier> supplierRepository, IRepository<StockPilot.Entity.Country> countryRepository, IRepository<StockPilot.Entity.Currency> currencyRepository, IRepository<StockPilot.Entity.PurchaseInvoice> purchaseInvoiceRepository, IRepository<StockPilot.Entity.ItemMaster> itemRepository)
		{
			_supplierRepository = supplierRepository;
			_countryRepository = countryRepository;
			_currencyRepository = currencyRepository;
			_purchaseInvoiceRepository = purchaseInvoiceRepository;
			_itemRepository = itemRepository;
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Suppliers" })]
		public async Task<PagedResultDto<SupplierListDto>> GetAll(SupplierGridInput input)
		{
			using (IncludeDeleted(input.IncludeDeleted))
			{
				IQueryable<StockPilot.Entity.Supplier> query = FilteredQuery(input);
				int totalCount = await query.CountAsync();
				List<StockPilot.Entity.Supplier> rows = await MasterDataAppServiceBase.ApplySorting(query, input.Sorting, "Name ascending", SortFields).PageBy(input).ToListAsync();
				return new PagedResultDto<SupplierListDto>(totalCount, base.ObjectMapper.Map<List<SupplierListDto>>(rows));
			}
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Suppliers" })]
		[RequiresFeature(new string[] { "App.Inventory.Export" })]
		public async Task<FileDto> GetAllToExcel(SupplierGridInput input)
		{
			using (IncludeDeleted(input.IncludeDeleted))
			{
				List<StockPilot.Entity.Supplier> rows = await MasterDataAppServiceBase.ApplySorting(FilteredQuery(input), input.Sorting, "Name ascending", SortFields).Take(100000).ToListAsync();
				return ExcelExporter.Export(base.ObjectMapper.Map<List<SupplierListDto>>(rows));
			}
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Suppliers" })]
		public async Task<EditSupplierDto> GetForEdit(EntityDto input)
		{
			IObjectMapper objectMapper = base.ObjectMapper;
			return objectMapper.Map<EditSupplierDto>(await _supplierRepository.GetAsync(input.Id));
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Suppliers.Create" })]
		public async Task<EntityDto> Create(CreateSupplierDto input)
		{
			input.Code = input.Code.Trim();
			await EnsureCodeIsFreeAsync(input.Code, null);
			await EnsureReferencesExistAsync(input.CountryId, input.CurrencyId);
			return new EntityDto(await _supplierRepository.InsertAndGetIdAsync(MapForCreate<StockPilot.Entity.Supplier>(input)));
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Suppliers.Edit" })]
		public async Task Update(EditSupplierDto input)
		{
			input.Code = input.Code.Trim();
			StockPilot.Entity.Supplier entity = await _supplierRepository.GetAsync(input.Id);
			await EnsureCodeIsFreeAsync(input.Code, entity.Id);
			await EnsureReferencesExistAsync(input.CountryId, input.CurrencyId);
			MapForUpdate(input, entity);
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Suppliers.Delete" })]
		public async Task Delete(EntityDto input)
		{
			StockPilot.Entity.Supplier entity = await _supplierRepository.GetAsync(input.Id);
			if (await _purchaseInvoiceRepository.CountAsync((StockPilot.Entity.PurchaseInvoice d) => d.SupplierId == entity.Id) > 0)
			{
				throw new UserFriendlyException(L("SupplierHasInvoicesDeactivateInstead", entity.Name));
			}
			if (await _itemRepository.CountAsync((StockPilot.Entity.ItemMaster i) => i.DefaultSupplierId == (int?)entity.Id) > 0)
			{
				throw InUse(entity.Name, L("Items"));
			}
			await _supplierRepository.DeleteAsync(entity);
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Suppliers.Delete" })]
		public async Task Restore(EntityDto input)
		{
			await RestoreEntityAsync(_supplierRepository, input.Id, (StockPilot.Entity.Supplier entity) => EnsureCodeIsFreeAsync(entity.Code, entity.Id));
		}

		public async Task<ListResultDto<SupplierComboDto>> GetForCombo()
		{
			List<StockPilot.Entity.Supplier> rows = await (from s in _supplierRepository.GetAll()
				where s.IsActive
				orderby s.Name
				select s).ToListAsync();
			return new ListResultDto<SupplierComboDto>(base.ObjectMapper.Map<List<SupplierComboDto>>(rows));
		}

		private async Task EnsureCodeIsFreeAsync(string code, int? exceptId)
		{
			if (await _supplierRepository.CountAsync((StockPilot.Entity.Supplier s) => s.Code == code && (int?)s.Id != exceptId) > 0)
			{
				throw CodeExists(code);
			}
		}

		private async Task EnsureReferencesExistAsync(int? countryId, int? currencyId)
		{
			bool flag = countryId.HasValue;
			if (flag)
			{
				flag = await _countryRepository.FirstOrDefaultAsync(countryId.Value) == null;
			}
			if (flag)
			{
				throw new UserFriendlyException(L("CountryNotFound"));
			}
			flag = currencyId.HasValue;
			if (flag)
			{
				flag = await _currencyRepository.FirstOrDefaultAsync(currencyId.Value) == null;
			}
			if (flag)
			{
				throw new UserFriendlyException(L("CurrencyNotFound"));
			}
		}

		private IQueryable<StockPilot.Entity.Supplier> FilteredQuery(SupplierGridInput input)
		{
			return _supplierRepository.GetAll().Include((StockPilot.Entity.Supplier s) => s.CountryInfo).Include((StockPilot.Entity.Supplier s) => s.CurrencyInfo)
				.WhereIf(!input.Filter.IsNullOrWhiteSpace(), (StockPilot.Entity.Supplier s) => s.Code.Contains(input.Filter) || s.Name.Contains(input.Filter) || s.ContactPerson.Contains(input.Filter) || s.Email.Contains(input.Filter) || s.Phone.Contains(input.Filter) || s.City.Contains(input.Filter))
				.WhereIf(input.CountryId.HasValue, (StockPilot.Entity.Supplier s) => s.CountryId == (int?)input.CountryId.Value)
				.WhereIf(input.CurrencyId.HasValue, (StockPilot.Entity.Supplier s) => s.CurrencyId == (int?)input.CurrencyId.Value)
				.WhereIf(input.IsActive.HasValue, (StockPilot.Entity.Supplier s) => s.IsActive == input.IsActive.Value);
		}
	}
}
