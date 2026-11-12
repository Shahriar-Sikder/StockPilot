using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Session;
using Abp.UI;
using StockPilot.Entity;

namespace StockPilot.Inventory
{
	public class DocumentNumberManager : StockPilotDomainServiceBase
	{
		public const int MaxAttempts = 5;

		private static readonly Dictionary<DocumentNumberType, string> Prefixes = new Dictionary<DocumentNumberType, string>
		{
			{
				DocumentNumberType.PurchaseInvoice,
				"PI"
			},
			{
				DocumentNumberType.IssueSale,
				"IS"
			}
		};

		private readonly IRepository<DocumentSequence> _sequenceRepository;

		private readonly IRepository<PurchaseInvoice> _purchaseInvoiceRepository;

		private readonly IRepository<IssueSale> _issueSaleRepository;

		public IAbpSession AbpSession { get; set; }

		public DocumentNumberManager(IRepository<DocumentSequence> sequenceRepository, IRepository<PurchaseInvoice> purchaseInvoiceRepository, IRepository<IssueSale> issueSaleRepository)
		{
			_sequenceRepository = sequenceRepository;
			_purchaseInvoiceRepository = purchaseInvoiceRepository;
			_issueSaleRepository = issueSaleRepository;
			AbpSession = NullAbpSession.Instance;
		}

		public static string GetPrefix(DocumentNumberType type)
		{
			return Prefixes[type];
		}

		public static string Format(DocumentNumberType type, int number)
		{
			return GetPrefix(type) + "-" + number.ToString("D4", CultureInfo.InvariantCulture);
		}

		public virtual async Task<string> GetNextNumberAsync(DocumentNumberType type)
		{
			int tenantId = AbpSession.GetTenantId();
			int attempt = 1;
			while (true)
			{
				try
				{
					return await ReserveNumberAsync(type, tenantId);
				}
				catch (Exception exception) when (attempt < 5 && DbExceptionHelper.IsSaveConflict(exception))
				{
					base.Logger.Debug("Document number conflict for " + type.ToString() + ", attempt " + attempt + ". Retrying.");
				}
				catch (Exception exception2) when (DbExceptionHelper.IsSaveConflict(exception2))
				{
					base.Logger.Warn("Could not reserve a document number for " + type.ToString() + " after " + 5 + " attempts.", exception2);
					throw new UserFriendlyException(L("DocumentNumberBusy"));
				}
				attempt++;
			}
		}

		private async Task<string> ReserveNumberAsync(DocumentNumberType type, int tenantId)
		{
			string documentType = GetPrefix(type);
			using (IUnitOfWorkCompleteHandle uow = base.UnitOfWorkManager.Begin(TransactionScopeOption.RequiresNew))
			{
				using (base.CurrentUnitOfWork.SetTenantId(tenantId))
				{
					DocumentSequence sequence = await _sequenceRepository.FirstOrDefaultAsync((DocumentSequence s) => s.DocumentType == documentType);
					if (sequence == null)
					{
						DocumentSequence documentSequence = new DocumentSequence
						{
							TenantId = tenantId,
							DocumentType = documentType
						};
						DocumentSequence documentSequence2 = documentSequence;
						documentSequence2.NextNumber = await GetHighestUsedNumberAsync(type) + 1;
						sequence = documentSequence;
						await _sequenceRepository.InsertAsync(sequence);
					}
					int number = Math.Max(sequence.NextNumber, 1);
					string candidate = Format(type, number);
					while (await IsNumberUsedAsync(type, candidate))
					{
						number++;
						candidate = Format(type, number);
					}
					sequence.NextNumber = number + 1;
					await base.CurrentUnitOfWork.SaveChangesAsync();
					await uow.CompleteAsync();
					return candidate;
				}
			}
		}

		private async Task<bool> IsNumberUsedAsync(DocumentNumberType type, string number)
		{
			using (base.CurrentUnitOfWork.DisableFilter("SoftDelete"))
			{
				return (type != DocumentNumberType.PurchaseInvoice) ? (await _issueSaleRepository.CountAsync((IssueSale d) => d.DocumentNo == number) > 0) : (await _purchaseInvoiceRepository.CountAsync((PurchaseInvoice d) => d.InvoiceNo == number) > 0);
			}
		}

		private async Task<int> GetHighestUsedNumberAsync(DocumentNumberType type)
		{
			string prefix = GetPrefix(type) + "-";
			List<string> numbers;
			using (base.CurrentUnitOfWork.DisableFilter("SoftDelete"))
			{
				List<string> list = ((type != DocumentNumberType.PurchaseInvoice) ? (await _issueSaleRepository.GetAllListAsync((IssueSale d) => d.DocumentNo.StartsWith(prefix))).Select((IssueSale d) => d.DocumentNo).ToList() : (await _purchaseInvoiceRepository.GetAllListAsync((PurchaseInvoice d) => d.InvoiceNo.StartsWith(prefix))).Select((PurchaseInvoice d) => d.InvoiceNo).ToList());
				numbers = list;
			}
			int highest = 0;
			foreach (string item in numbers)
			{
				if (int.TryParse(item.Substring(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) && parsed > highest)
				{
					highest = parsed;
				}
			}
			return highest;
		}
	}
}
