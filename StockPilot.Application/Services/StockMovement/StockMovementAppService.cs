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
using StockPilot.Dto;
using StockPilot.Entity;
using StockPilot.Services.Common;
using StockPilot.Services.StockMovement.Dtos;
using StockPilot.Services.StockMovement.Exporting;

namespace StockPilot.Services.StockMovement
{
	[AbpAuthorize(new string[] { })]
	public class StockMovementAppService : MasterDataAppServiceBase, IStockMovementAppService, IApplicationService, ITransientDependency
	{
		private const string DefaultSorting = "Id descending";

		private static readonly Dictionary<string, string> SortFields = new Dictionary<string, string>
		{
			{ "movementDate", "MovementDate" },
			{ "itemCode", "ItemCode" },
			{ "itemName", "ItemName" },
			{ "movementType", "MovementType" },
			{ "quantity", "Quantity" },
			{ "unitCost", "UnitCost" },
			{ "totalValue", "TotalValue" },
			{ "balanceAfter", "BalanceAfter" },
			{ "referenceNo", "ReferenceNo" },
			{ "creationTime", "CreationTime" },
			{ "id", "Id" }
		};

		private readonly IRepository<StockPilot.Entity.StockMovement, long> _movementRepository;

		public StockMovementExcelExporter ExcelExporter { get; set; }

		public StockMovementAppService(IRepository<StockPilot.Entity.StockMovement, long> movementRepository)
		{
			_movementRepository = movementRepository;
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.StockHistory" })]
		public async Task<PagedResultDto<StockMovementListDto>> GetAll(StockMovementGridInput input)
		{
			using (base.CurrentUnitOfWork.DisableFilter("SoftDelete"))
			{
				IQueryable<StockMovementListDto> query = FilteredQuery(input);
				return new PagedResultDto<StockMovementListDto>(await query.CountAsync(), await MasterDataAppServiceBase.ApplySorting(query, input.Sorting, "Id descending", SortFields).PageBy(input).ToListAsync());
			}
		}

		[AbpAuthorize(new string[] { "Pages.StockPilot.StockHistory" })]
		[RequiresFeature(new string[] { "App.Inventory.Export" })]
		public async Task<FileDto> GetAllToExcel(StockMovementGridInput input)
		{
			using (base.CurrentUnitOfWork.DisableFilter("SoftDelete"))
			{
				List<StockMovementListDto> rows = await MasterDataAppServiceBase.ApplySorting(FilteredQuery(input), input.Sorting, "Id descending", SortFields).Take(100000).ToListAsync();
				return ExcelExporter.Export(rows);
			}
		}

		private IQueryable<StockMovementListDto> FilteredQuery(StockMovementGridInput input)
		{
			DateTime? dateFrom = input.DateFrom?.Date;
			DateTime? dateToExclusive = input.DateTo?.Date.AddDays(1.0);
			string referenceType = input.ReferenceType?.Trim();
			return (from m in _movementRepository.GetAll()
				select new StockMovementListDto
				{
					Id = m.Id,
					MovementDate = m.MovementDate,
					ItemMasterId = m.ItemMasterId,
					ItemCode = m.ItemMasterInfo.ItemCode,
					ItemName = m.ItemMasterInfo.ItemName,
					BaseUnitCode = m.ItemMasterInfo.BaseUnit.Code,
					MovementType = m.MovementType,
					Quantity = m.Quantity,
					UnitCost = m.UnitCost,
					TotalValue = m.TotalValue,
					BalanceAfter = m.BalanceAfter,
					ReferenceType = m.ReferenceType,
					ReferenceId = m.ReferenceId,
					ReferenceNo = m.ReferenceNo,
					Remarks = m.Remarks,
					CreatorUserId = m.CreatorUserId,
					CreationTime = m.CreationTime
				}).WhereIf(!input.Filter.IsNullOrWhiteSpace(), (StockMovementListDto m) => m.ItemCode.Contains(input.Filter) || m.ItemName.Contains(input.Filter) || m.ReferenceNo.Contains(input.Filter) || m.Remarks.Contains(input.Filter)).WhereIf(dateFrom.HasValue, (StockMovementListDto m) => m.MovementDate >= ((DateTime?)dateFrom).Value).WhereIf(dateToExclusive.HasValue, (StockMovementListDto m) => m.MovementDate < ((DateTime?)dateToExclusive).Value)
				.WhereIf(input.ItemMasterId.HasValue, (StockMovementListDto m) => m.ItemMasterId == input.ItemMasterId.Value)
				.WhereIf(input.MovementType.HasValue, (StockMovementListDto m) => (int)m.MovementType == (int)input.MovementType.Value)
				.WhereIf(!referenceType.IsNullOrEmpty(), (StockMovementListDto m) => m.ReferenceType == referenceType)
				.WhereIf(input.ReferenceId.HasValue, (StockMovementListDto m) => m.ReferenceId == (int?)input.ReferenceId.Value);
		}
	}
}
