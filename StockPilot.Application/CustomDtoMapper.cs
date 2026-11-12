using AutoMapper;
using StockPilot.Authorization.Users;
using StockPilot.Authorization.Users.Dto;
using StockPilot.Entity;
using StockPilot.Inventory;
using StockPilot.Services.ItemMaster;
using StockPilot.Services.ItemMaster.Dtos;
using StockPilot.Services.ItemSubCategory.Dtos;
using StockPilot.Services.Supplier.Dtos;
using StockPilot.Services.UnitConversion.Dtos;
using StockPilot.Services.UnitMaster.Dtos;

namespace StockPilot
{
	internal static class CustomDtoMapper
	{
		private static volatile bool _mappedBefore;

		private static readonly object SyncObj = new object();

		public static void CreateMappings(IMapperConfigurationExpression mapper)
		{
			lock (SyncObj)
			{
				if (!_mappedBefore)
				{
					CreateMappingsInternal(mapper);
					_mappedBefore = true;
				}
			}
		}

		private static void CreateMappingsInternal(IMapperConfigurationExpression mapper)
		{
			mapper.CreateMap<User, UserEditDto>().ForMember((UserEditDto dto) => dto.Password, delegate(IMemberConfigurationExpression<User, UserEditDto, string> options)
			{
				options.Ignore();
			}).ReverseMap()
				.ForMember((User user) => user.Password, delegate(IMemberConfigurationExpression<UserEditDto, User, string> options)
				{
					options.Ignore();
				});
			CreateInventoryMappings(mapper);
		}

		private static void CreateInventoryMappings(IMapperConfigurationExpression mapper)
		{
			mapper.ForAllMaps(delegate(TypeMap typeMap, IMappingExpression map)
			{
				if (Is<UnitMaster, UnitMasterListDto>(typeMap))
				{
					map.ForMember("UnitSetName", delegate(IMemberConfigurationExpression o)
					{
						o.MapFrom((object s) => (((UnitMaster)s).UnitSetInfo == null) ? null : ((UnitMaster)s).UnitSetInfo.Name);
					});
				}
				else if (Is<UnitConversion, UnitConversionListDto>(typeMap))
				{
					map.ForMember("UnitSetName", delegate(IMemberConfigurationExpression o)
					{
						o.MapFrom((object s) => (((UnitConversion)s).UnitSetInfo == null) ? null : ((UnitConversion)s).UnitSetInfo.Name);
					});
				}
				else if (Is<ItemSubCategory, ItemSubCategoryListDto>(typeMap))
				{
					map.ForMember("ItemCategoryName", delegate(IMemberConfigurationExpression o)
					{
						o.MapFrom((object s) => (((ItemSubCategory)s).ItemCategoryInfo == null) ? null : ((ItemSubCategory)s).ItemCategoryInfo.Name);
					});
				}
				else if (Is<ItemMasterGridRow, ItemMasterListDto>(typeMap))
				{
					map.ForMember("Status", delegate(IMemberConfigurationExpression o)
					{
						o.MapFrom((object s) => (ItemStockStatus)((ItemMasterGridRow)s).StatusCode);
					});
				}
				else if (Is<Supplier, SupplierListDto>(typeMap))
				{
					map.ForMember("CountryName", delegate(IMemberConfigurationExpression o)
					{
						o.MapFrom((object s) => (((Supplier)s).CountryInfo == null) ? null : ((Supplier)s).CountryInfo.Name);
					});
					map.ForMember("CurrencyCode", delegate(IMemberConfigurationExpression o)
					{
						o.MapFrom((object s) => (((Supplier)s).CurrencyInfo == null) ? null : ((Supplier)s).CurrencyInfo.Code);
					});
				}
			});
		}

		private static bool Is<TSource, TDestination>(TypeMap typeMap)
		{
			if (typeMap.SourceType == typeof(TSource))
			{
				return typeMap.DestinationType == typeof(TDestination);
			}
			return false;
		}
	}
}
