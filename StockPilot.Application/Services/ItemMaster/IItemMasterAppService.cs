using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Dependency;
using StockPilot.Dto;
using StockPilot.Services.Common.Dtos;
using StockPilot.Services.ItemMaster.Dtos;

namespace StockPilot.Services.ItemMaster
{
	public interface IItemMasterAppService : IApplicationService, ITransientDependency
	{
		Task<ItemMasterPagedResultDto> GetAll(ItemMasterGridInput input);

		Task<FileDto> GetAllToExcel(ItemMasterGridInput input);

		Task<ItemDetailDto> GetItemDetail(EntityDto input);

		Task<ListResultDto<ItemSearchResultDto>> SearchForDocument(SearchItemsInput input);

		Task<EditItemMasterDto> GetForEdit(EntityDto input);

		Task<EntityDto> Create(CreateItemMasterDto input);

		Task Update(EditItemMasterDto input);

		Task Delete(EntityDto input);

		Task Restore(EntityDto input);

		Task<ListResultDto<ItemMasterComboDto>> GetForCombo();

		Task<PictureUrlDto> UploadPicture(UploadPictureInput input);


		Task DeletePicture(EntityDto input);
	}
}
