using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
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
using Abp.Threading;
using Abp.UI;
using StockPilot.Dto;
using StockPilot.Entity;
using StockPilot.Inventory;
using StockPilot.Services.Common;
using StockPilot.Services.PurchaseInvoice.Dtos;
using StockPilot.Services.PurchaseInvoice.Exporting;
using StockPilot.Storage;

namespace StockPilot.Services.PurchaseInvoice
{
	[AbpAuthorize(new string[] { })]
	[RequiresFeature(new string[] { "App.Inventory.Purchases" })]
	public class PurchaseInvoiceAppService : DocumentAppServiceBase, IPurchaseInvoiceAppService, IApplicationService, ITransientDependency
	{
		private static readonly Dictionary<string, string> SortFields = new Dictionary<string, string>
		{
			{ "invoiceNo", "InvoiceNo" },
			{ "invoiceDate", "InvoiceDate" },
			{ "supplierName", "SupplierName" },
			{ "supplierInvoiceNo", "SupplierInvoiceNo" },
			{ "totalAmount", "TotalAmount" },
			{ "status", "Status" },
			{ "lineCount", "LineCount" },
			{ "attachmentCount", "AttachmentCount" },
			{ "postedTime", "PostedTime" },
			{ "creationTime", "CreationTime" }
		};

		private readonly IRepository<StockPilot.Entity.PurchaseInvoice> _invoiceRepository;

		private readonly IRepository<PurchaseInvoiceItem> _lineRepository;

		private readonly IRepository<PurchaseInvoiceDocument> _documentRepository;

		private readonly IRepository<StockPilot.Entity.Supplier> _supplierRepository;

		private readonly IBinaryObjectManager _binaryObjectManager;

		private readonly DocumentFileStore _documentFiles;

		private readonly DocumentNumberManager _documentNumberManager;

		private readonly StockManager _stockManager;

		public PurchaseInvoiceExcelExporter ExcelExporter { get; set; }

		public PurchaseInvoiceAppService(IRepository<StockPilot.Entity.PurchaseInvoice> invoiceRepository, IRepository<PurchaseInvoiceItem> lineRepository, IRepository<PurchaseInvoiceDocument> documentRepository, IRepository<StockPilot.Entity.Supplier> supplierRepository, IBinaryObjectManager binaryObjectManager, DocumentNumberManager documentNumberManager, StockManager stockManager, DocumentFileStore documentFiles)
		{
			_invoiceRepository = invoiceRepository;
			_lineRepository = lineRepository;
			_documentRepository = documentRepository;
			_supplierRepository = supplierRepository;
			_binaryObjectManager = binaryObjectManager;
			_documentFiles = documentFiles;
			_documentNumberManager = documentNumberManager;
			_stockManager = stockManager;
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Purchases" })]
		public async Task<PurchaseInvoicePagedResultDto> GetAll(PurchaseInvoiceGridInput input)
		{
			using (IncludeDeleted(input.IncludeDeleted))
			{
				IQueryable<PurchaseInvoiceGridRow> baseQuery = FilteredQuery(input);
				var counts = await (from r in baseQuery
					group r by r.Status into g
					select new
					{
						Status = g.Key,
						Count = g.Count()
					}).ToListAsync();
				IQueryable<PurchaseInvoiceGridRow> query = baseQuery.WhereIf(input.Status.HasValue, (PurchaseInvoiceGridRow r) => (int)r.Status == (int)input.Status.Value);
				int totalCount = await query.CountAsync();
				List<PurchaseInvoiceGridRow> rows = await MasterDataAppServiceBase.ApplySorting(query, input.Sorting, "InvoiceDate descending, InvoiceNo descending", SortFields).PageBy(input).ToListAsync();
				return new PurchaseInvoicePagedResultDto(totalCount, base.ObjectMapper.Map<List<PurchaseInvoiceListDto>>(rows))
				{
					AllCount = counts.Sum(c => c.Count),
					DraftCount = counts.Where(c => c.Status == DocumentStatus.Draft).Sum(c => c.Count),
					PostedCount = counts.Where(c => c.Status == DocumentStatus.Posted).Sum(c => c.Count),
					CancelledCount = counts.Where(c => c.Status == DocumentStatus.Cancelled).Sum(c => c.Count)
				};
			}
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Purchases" })]
		[RequiresFeature(new string[] { "App.Inventory.Export" })]
		public async Task<FileDto> GetAllToExcel(PurchaseInvoiceGridInput input)
		{
			using (IncludeDeleted(input.IncludeDeleted))
			{
				List<PurchaseInvoiceGridRow> rows = await MasterDataAppServiceBase.ApplySorting(FilteredQuery(input).WhereIf(input.Status.HasValue, (PurchaseInvoiceGridRow r) => (int)r.Status == (int)input.Status.Value), input.Sorting, "InvoiceDate descending, InvoiceNo descending", SortFields).Take(100000).ToListAsync();
				return ExcelExporter.Export(base.ObjectMapper.Map<List<PurchaseInvoiceListDto>>(rows));
			}
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Purchases" })]
		public async Task<EditPurchaseInvoiceDto> Get(EntityDto input)
		{
			StockPilot.Entity.PurchaseInvoice invoice = await _invoiceRepository.GetAll().Include((StockPilot.Entity.PurchaseInvoice d) => d.SupplierInfo).Include((StockPilot.Entity.PurchaseInvoice d) => d.CurrencyInfo)
				.FirstOrDefaultAsync((StockPilot.Entity.PurchaseInvoice d) => d.Id == input.Id);
			if (invoice == null)
			{
				throw new EntityNotFoundException(typeof(StockPilot.Entity.PurchaseInvoice), input.Id);
			}
			EditPurchaseInvoiceDto dto = base.ObjectMapper.Map<EditPurchaseInvoiceDto>(invoice);
			dto.SupplierName = invoice.SupplierInfo?.Name;
			dto.CurrencyCode = invoice.CurrencyInfo?.Code;
			List<PurchaseInvoiceItem> lines;
			using (base.CurrentUnitOfWork.DisableFilter("SoftDelete"))
			{
				lines = await (from l in _lineRepository.GetAll().Include((PurchaseInvoiceItem l) => l.ItemMasterInfo.BaseUnit).Include((PurchaseInvoiceItem l) => l.UnitMasterInfo)
					where l.PurchaseInvoiceId == invoice.Id && !l.IsDeleted
					orderby l.Id
					select l).ToListAsync();
			}
			dto.Lines = lines.Select(delegate(PurchaseInvoiceItem l)
			{
				EditPurchaseInvoiceItemDto editPurchaseInvoiceItemDto = base.ObjectMapper.Map<EditPurchaseInvoiceItemDto>(l);
				editPurchaseInvoiceItemDto.ItemCode = l.ItemMasterInfo?.ItemCode;
				editPurchaseInvoiceItemDto.ItemName = l.ItemMasterInfo?.ItemName;
				editPurchaseInvoiceItemDto.ItemPictureUrl = l.ItemMasterInfo?.PictureUrl;
				editPurchaseInvoiceItemDto.BaseUnitCode = l.ItemMasterInfo?.BaseUnit?.Code;
				editPurchaseInvoiceItemDto.UnitCode = l.UnitMasterInfo?.Code;
				return editPurchaseInvoiceItemDto;
			}).ToList();
			List<PurchaseInvoiceDocument> documents = await _documentRepository.GetAllListAsync((PurchaseInvoiceDocument x) => x.PurchaseInvoiceId == invoice.Id);
			dto.Attachments = base.ObjectMapper.Map<List<PurchaseInvoiceDocumentDto>>(documents.OrderBy((PurchaseInvoiceDocument x) => x.Id).ToList());
			return dto;
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Purchases.Create" })]
		public async Task<EntityDto> Create(CreatePurchaseInvoiceDto input)
		{
			StockPilot.Entity.Currency currency = await GetCurrencyAsync(input.CurrencyId);
			await EnsureSupplierAsync(input.SupplierId);
			StockPilot.Entity.PurchaseInvoice invoice = MapForCreate<StockPilot.Entity.PurchaseInvoice>(input);
			invoice.InvoiceDate = input.InvoiceDate.Date;
			StockPilot.Entity.PurchaseInvoice purchaseInvoice = invoice;
			purchaseInvoice.InvoiceNo = await GetInvoiceNoAsync(input.InvoiceNo);
			purchaseInvoice = invoice;
			purchaseInvoice.ExchangeRate = await ResolveExchangeRateAsync(currency, input.ExchangeRate, invoice.InvoiceDate);
			ResetPosting(invoice);
			Dictionary<int, StockPilot.Entity.ItemMaster> items = new Dictionary<int, StockPilot.Entity.ItemMaster>();
			List<PurchaseInvoiceItem> lines = new List<PurchaseInvoiceItem>();
			foreach (CreatePurchaseInvoiceItemDto lineInput in input.Lines ?? new List<CreatePurchaseInvoiceItemDto>())
			{
				PurchaseInvoiceItem line = MapForCreate<PurchaseInvoiceItem>(lineInput);
				await PrepareLineAsync(line, items, currency);
				lines.Add(line);
			}
			DocumentTotals.CalculateHeader(invoice, lines, currency.DecimalPlaces);
			int id = await _invoiceRepository.InsertAndGetIdAsync(invoice);
			foreach (PurchaseInvoiceItem line2 in lines)
			{
				line2.PurchaseInvoiceId = id;
				await _lineRepository.InsertAsync(line2);
			}
			return new EntityDto(id);
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Purchases.Edit" })]
		public async Task Update(EditPurchaseInvoiceDto input)
		{
			StockPilot.Entity.PurchaseInvoice invoice = await _invoiceRepository.GetAsync(input.Id);
			EnsureDraft(invoice.Status, invoice.InvoiceNo);
			StockPilot.Entity.Currency currency = await GetCurrencyAsync(input.CurrencyId);
			await EnsureSupplierAsync(input.SupplierId);
			string invoiceNo = invoice.InvoiceNo;
			MapForUpdate(input, invoice);
			invoice.InvoiceNo = invoiceNo;
			invoice.InvoiceDate = input.InvoiceDate.Date;
			StockPilot.Entity.PurchaseInvoice purchaseInvoice = invoice;
			purchaseInvoice.ExchangeRate = await ResolveExchangeRateAsync(currency, input.ExchangeRate, invoice.InvoiceDate);
			ResetPosting(invoice);
			List<PurchaseInvoiceItem> existing = await _lineRepository.GetAllListAsync((PurchaseInvoiceItem l) => l.PurchaseInvoiceId == invoice.Id);
			List<EditPurchaseInvoiceItemDto> inputLines = input.Lines ?? new List<EditPurchaseInvoiceItemDto>();
			HashSet<int> keptIds = new HashSet<int>(from l in inputLines
				where l.Id > 0
				select l.Id);
			foreach (PurchaseInvoiceItem removed in existing.Where((PurchaseInvoiceItem l) => !keptIds.Contains(l.Id)))
			{
				await _lineRepository.DeleteAsync(removed);
			}
			Dictionary<int, StockPilot.Entity.ItemMaster> items = new Dictionary<int, StockPilot.Entity.ItemMaster>();
			List<PurchaseInvoiceItem> lines = new List<PurchaseInvoiceItem>();
			foreach (EditPurchaseInvoiceItemDto lineInput in inputLines)
			{
				PurchaseInvoiceItem line;
				if (lineInput.Id > 0)
				{
					line = existing.FirstOrDefault((PurchaseInvoiceItem l) => l.Id == lineInput.Id);
					if (line == null)
					{
						throw new UserFriendlyException(L("DocumentLineNotFound"));
					}
					MapForUpdate(lineInput, line);
				}
				else
				{
					line = MapForCreate<PurchaseInvoiceItem>(lineInput);
					await _lineRepository.InsertAsync(line);
				}
				line.PurchaseInvoiceId = invoice.Id;
				await PrepareLineAsync(line, items, currency);
				lines.Add(line);
			}
			DocumentTotals.CalculateHeader(invoice, lines, currency.DecimalPlaces);
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Purchases.Delete" })]
		public async Task DeleteDraft(EntityDto input)
		{
			StockPilot.Entity.PurchaseInvoice invoice = await _invoiceRepository.GetAsync(input.Id);
			EnsureDraft(invoice.Status, invoice.InvoiceNo);
			foreach (PurchaseInvoiceItem line in await _lineRepository.GetAllListAsync((PurchaseInvoiceItem l) => l.PurchaseInvoiceId == invoice.Id))
			{
				await _lineRepository.DeleteAsync(line);
			}
			foreach (PurchaseInvoiceDocument document in await _documentRepository.GetAllListAsync((PurchaseInvoiceDocument x) => x.PurchaseInvoiceId == invoice.Id))
			{
				await _documentRepository.DeleteAsync(document);
			}
			await _invoiceRepository.DeleteAsync(invoice);
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Purchases.Post" })]
		public async Task Post(EntityDto input)
		{
			await _stockManager.PostPurchaseAsync(input.Id);
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Purchases.Cancel" })]
		public async Task Cancel(EntityDto input)
		{
			await _stockManager.CancelPurchaseAsync(input.Id);
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Purchases.Edit" })]
		[RequiresFeature(new string[] { "App.Inventory.Purchases", "App.Inventory.Purchases.Attachments" }, RequiresAll = true)]
		public async Task<PurchaseInvoiceDocumentDto> UploadDocument(UploadPurchaseDocumentInput input)
		{
			StockPilot.Entity.PurchaseInvoice invoice = await _invoiceRepository.GetAsync(input.PurchaseInvoiceId);
			string fileName = PurchaseDocumentRules.CleanFileName(input.FileName);
			string contentType = PurchaseDocumentRules.GetContentTypeOrNull(fileName);
			if (string.IsNullOrEmpty(fileName) || contentType == null)
			{
				throw new UserFriendlyException(L("AttachmentTypeNotAllowed", string.Join(", ", PurchaseDocumentRules.AllowedExtensions)));
			}
			byte[] bytes = PictureHelper.DecodeBase64(input.Base64Content);
			if (bytes == null || bytes.Length == 0)
			{
				throw new UserFriendlyException(L("AttachmentEmptyOrInvalid", fileName));
			}
			if (bytes.Length > 3145728)
			{
				throw new UserFriendlyException(L("AttachmentTooLarge", fileName, 3));
			}
			if (!PurchaseDocumentRules.ContentMatchesExtension(fileName, bytes))
			{
				throw new UserFriendlyException(L("AttachmentContentDoesNotMatchType", fileName));
			}
			// A file in the picture folder (documents\t{tenant}\), not a database row. Written now; deleted again if the
			// document is not saved.
			string filePath = await _documentFiles.SaveAsync(invoice.TenantId, bytes, Path.GetExtension(fileName));
			base.CurrentUnitOfWork.Failed += (sender, args) => DeleteDocumentFile(filePath);
			PurchaseInvoiceDocument document = new PurchaseInvoiceDocument
			{
				TenantId = invoice.TenantId,
				PurchaseInvoiceId = invoice.Id,
				FileName = fileName,
				ContentType = contentType,
				SizeBytes = bytes.Length,
				FilePath = filePath,
				BinaryObjectId = null
			};
			await _documentRepository.InsertAndGetIdAsync(document);
			return base.ObjectMapper.Map<PurchaseInvoiceDocumentDto>(document);
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Purchases" })]
		[RequiresFeature(new string[] { "App.Inventory.Purchases", "App.Inventory.Purchases.Attachments" }, RequiresAll = true)]
		public async Task<PurchaseDocumentContentDto> DownloadDocument(EntityDto input)
		{
			PurchaseInvoiceDocument document = await _documentRepository.GetAsync(input.Id);
			// The file; a document saved before the change is still read from the database until it is moved.
			byte[] content = null;
			if (!string.IsNullOrEmpty(document.FilePath))
			{
				content = await _documentFiles.ReadOrNullAsync(document.FilePath);
			}
			else if (document.BinaryObjectId.HasValue)
			{
				content = (await _binaryObjectManager.GetOrNullAsync(document.BinaryObjectId.Value))?.Bytes;
			}
			if (content == null)
			{
				throw new UserFriendlyException(L("AttachmentFileMissing", document.FileName));
			}
			return new PurchaseDocumentContentDto
			{
				FileName = document.FileName,
				ContentType = document.ContentType,
				Base64Content = Convert.ToBase64String(content)
			};
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Purchases.Edit" })]
		[RequiresFeature(new string[] { "App.Inventory.Purchases", "App.Inventory.Purchases.Attachments" }, RequiresAll = true)]
		public async Task DeleteDocument(EntityDto input)
		{
			await _documentRepository.DeleteAsync(input.Id);
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.Purchases.Edit" })]
		[RequiresFeature(new string[] { "App.Inventory.Purchases", "App.Inventory.Purchases.Attachments" }, RequiresAll = true)]
		public async Task RestoreDocument(EntityDto input)
		{
			await RestoreEntityAsync(_documentRepository, input.Id);
		}

		private void DeleteDocumentFile(string filePath)
		{
			try
			{
				AsyncHelper.RunSync(() => _documentFiles.DeleteAsync(filePath));
			}
			catch (Exception error)
			{
				base.Logger.Warn("Could not delete document file " + filePath + " after a failed save.", error);
			}
		}

		private IQueryable<PurchaseInvoiceGridRow> FilteredQuery(PurchaseInvoiceGridInput input)
		{
			DateTime? dateFrom = input.DateFrom?.Date;
			DateTime? dateToExclusive = DocumentAppServiceBase.EndOfDayExclusive(input.DateTo);
			return BuildGridQuery().WhereIf(!input.Filter.IsNullOrWhiteSpace(), (PurchaseInvoiceGridRow r) => r.InvoiceNo.Contains(input.Filter) || r.SupplierInvoiceNo.Contains(input.Filter) || r.SupplierName.Contains(input.Filter)).WhereIf(dateFrom.HasValue, (PurchaseInvoiceGridRow r) => r.InvoiceDate >= ((DateTime?)dateFrom).Value).WhereIf(dateToExclusive.HasValue, (PurchaseInvoiceGridRow r) => r.InvoiceDate < ((DateTime?)dateToExclusive).Value)
				.WhereIf(input.SupplierId.HasValue, (PurchaseInvoiceGridRow r) => r.SupplierId == input.SupplierId.Value);
		}

		private IQueryable<PurchaseInvoiceGridRow> BuildGridQuery()
		{
			return from d in _invoiceRepository.GetAll()
				select new PurchaseInvoiceGridRow
				{
					Id = d.Id,
					InvoiceNo = d.InvoiceNo,
					InvoiceDate = d.InvoiceDate,
					SupplierId = d.SupplierId,
					SupplierName = d.SupplierInfo.Name,
					SupplierInvoiceNo = d.SupplierInvoiceNo,
					CurrencyCode = d.CurrencyInfo.Code,
					ExchangeRate = d.ExchangeRate,
					SubTotal = d.SubTotal,
					DiscountAmount = d.DiscountAmount,
					TaxAmount = d.TaxAmount,
					OtherCharges = d.OtherCharges,
					TotalAmount = d.TotalAmount,
					Status = d.Status,
					PostedTime = d.PostedTime,
					LineCount = d.Items.Count((PurchaseInvoiceItem l) => !l.IsDeleted),
					AttachmentCount = d.Documents.Count((PurchaseInvoiceDocument x) => !x.IsDeleted),
					IsDeleted = d.IsDeleted,
					CreationTime = d.CreationTime
				};
		}

		private async Task PrepareLineAsync(PurchaseInvoiceItem line, Dictionary<int, StockPilot.Entity.ItemMaster> items, StockPilot.Entity.Currency currency)
		{
			StockPilot.Entity.ItemMaster item = await GetItemAsync(items, line.ItemMasterId);
			if (line.Quantity <= 0m)
			{
				throw new UserFriendlyException(L("LineQuantityMustBePositive", item.ItemName));
			}
			line.ConversionFactor = await GetFactorAsync(item, line.UnitMasterId);
			line.BaseQuantity = StockManager.Round(line.Quantity * line.ConversionFactor);
			DocumentTotals.CalculateLine(line, currency.DecimalPlaces);
			if (!DocumentTotals.IsDiscountValid(line.Quantity * line.UnitCost, line.DiscountAmount))
			{
				throw new UserFriendlyException(L("LineValueCannotBeNegative", item.ItemName));
			}
		}

		private async Task<string> GetInvoiceNoAsync(string requested)
		{
			if (string.IsNullOrWhiteSpace(requested))
			{
				return await _documentNumberManager.GetNextNumberAsync(DocumentNumberType.PurchaseInvoice);
			}
			string invoiceNo = requested.Trim();
			using (base.CurrentUnitOfWork.DisableFilter("SoftDelete"))
			{
				if (await _invoiceRepository.CountAsync((StockPilot.Entity.PurchaseInvoice d) => d.InvoiceNo == invoiceNo) > 0)
				{
					throw new UserFriendlyException(L("DocumentNumberAlreadyExists", invoiceNo));
				}
			}
			return invoiceNo;
		}

		private async Task EnsureSupplierAsync(int supplierId)
		{
			if (await _supplierRepository.FirstOrDefaultAsync(supplierId) == null)
			{
				throw new UserFriendlyException(L("SupplierNotFound"));
			}
		}

		private static void ResetPosting(StockPilot.Entity.PurchaseInvoice invoice)
		{
			invoice.Status = DocumentStatus.Draft;
			invoice.IsPosted = false;
			invoice.PostedBy = null;
			invoice.PostedTime = null;
		}
	}
}
