using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using StockPilot.Entity;

namespace StockPilot.Inventory
{
	public class UnitConversionManager : StockPilotDomainServiceBase
	{
		private readonly IRepository<ItemMaster> _itemRepository;

		private readonly IRepository<UnitMaster> _unitRepository;

		private readonly IRepository<UnitConversion> _conversionRepository;

		public UnitConversionManager(IRepository<ItemMaster> itemRepository, IRepository<UnitMaster> unitRepository, IRepository<UnitConversion> conversionRepository)
		{
			_itemRepository = itemRepository;
			_unitRepository = unitRepository;
			_conversionRepository = conversionRepository;
		}

		[UnitOfWork]
		public virtual async Task<decimal> GetFactorAsync(int itemId, int unitId)
		{
			return await GetFactorAsync(await _itemRepository.GetAsync(itemId), unitId);
		}

		[UnitOfWork]
		public virtual async Task<decimal> GetFactorAsync(ItemMaster item, int unitId)
		{
			if (unitId == item.BaseUnitId)
			{
				await EnsureBaseUnitInUnitSetAsync(item);
				return 1m;
			}
			ItemUnitInfo match = (await BuildUnitsAsync(item, includeInactive: true)).FirstOrDefault((ItemUnitInfo u) => u.UnitId == unitId);
			if (match != null)
			{
				return match.Factor;
			}
			UnitMaster unit = await _unitRepository.FirstOrDefaultAsync(unitId);
			if (unit == null)
			{
				throw new UserFriendlyException(L("UnitNotFound"));
			}
			if (unit.UnitSetId != item.UnitSetId)
			{
				throw new UserFriendlyException(L("UnitNotInItemUnitSet", unit.Code, item.ItemName));
			}
			throw new UserFriendlyException(L("NoConversionToBaseUnit", unit.Code, item.ItemName));
		}

		[UnitOfWork]
		public virtual async Task<List<ItemUnitInfo>> GetUnitsForItemAsync(int itemId)
		{
			return await BuildUnitsAsync(await _itemRepository.GetAsync(itemId), includeInactive: false);
		}

		private async Task<UnitMaster> EnsureBaseUnitInUnitSetAsync(ItemMaster item)
		{
			UnitMaster baseUnit = await _unitRepository.FirstOrDefaultAsync(item.BaseUnitId);
			if (baseUnit == null || baseUnit.UnitSetId != item.UnitSetId)
			{
				throw new UserFriendlyException(L("BaseUnitNotInUnitSet", item.ItemName));
			}
			return baseUnit;
		}

		private async Task<List<ItemUnitInfo>> BuildUnitsAsync(ItemMaster item, bool includeInactive)
		{
			UnitMaster baseUnit = await EnsureBaseUnitInUnitSetAsync(item);
			Dictionary<int, UnitMaster> units = (await _unitRepository.GetAllListAsync((UnitMaster u) => u.UnitSetId == item.UnitSetId)).ToDictionary((UnitMaster u) => u.Id);
			List<UnitConversion> conversions = (await _conversionRepository.GetAllListAsync((UnitConversion c) => c.UnitSetId == item.UnitSetId && c.IsActive && c.Factor > 0m)).Where((UnitConversion c) => units.ContainsKey(c.FromUnitId) && units.ContainsKey(c.ToUnitId)).ToList();
			Dictionary<int, decimal> factors = new Dictionary<int, decimal> { { baseUnit.Id, 1m } };
			Queue<int> queue = new Queue<int>();
			queue.Enqueue(baseUnit.Id);
			while (queue.Count > 0)
			{
				int current = queue.Dequeue();
				decimal currentFactor = factors[current];
				foreach (UnitConversion conversion in conversions)
				{
					if (conversion.ToUnitId == current && !factors.ContainsKey(conversion.FromUnitId))
					{
						factors[conversion.FromUnitId] = conversion.Factor * currentFactor;
						queue.Enqueue(conversion.FromUnitId);
					}
					else if (conversion.FromUnitId == current && !factors.ContainsKey(conversion.ToUnitId))
					{
						factors[conversion.ToUnitId] = currentFactor / conversion.Factor;
						queue.Enqueue(conversion.ToUnitId);
					}
				}
			}
			return (from pair in factors
				select new
				{
					Unit = units[pair.Key],
					Factor = pair.Value
				} into x
				where includeInactive || x.Unit.IsActive || x.Unit.Id == baseUnit.Id
				orderby x.Unit.Id == baseUnit.Id descending, x.Factor, x.Unit.Sequence
				select new ItemUnitInfo
				{
					UnitId = x.Unit.Id,
					Code = x.Unit.Code,
					Name = x.Unit.Name,
					Factor = x.Factor,
					IsBaseUnit = (x.Unit.Id == baseUnit.Id),
					IsActive = x.Unit.IsActive
				}).ToList();
		}
	}
}
