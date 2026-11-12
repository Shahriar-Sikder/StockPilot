using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Dependency;
using StockPilot.Dto;
using StockPilot.Services.Common.Dtos;
using StockPilot.Services.ItemCategory.Dtos;

namespace StockPilot.Services.ItemCategory
{
	public interface IItemCategoryAppService : IApplicationService, ITransientDependency
	{
		Task<PagedResultDto<ItemCategoryListDto>> GetAll(ItemCategoryGridInput input);

		Task<FileDto> GetAllToExcel(ItemCategoryGridInput input);

		Task<EditItemCategoryDto> GetForEdit(EntityDto input);

		Task<EntityDto> Create(CreateItemCategoryDto input);

		Task Update(EditItemCategoryDto input);

		Task Delete(EntityDto input);

		Task Restore(EntityDto input);

		Task<ListResultDto<ItemCategoryComboDto>> GetForCombo();

		Task<PictureDto> UploadPicture(UploadPictureInput input);

		Task<PictureDto> GetPicture(EntityDto input);

		Task DeletePicture(EntityDto input);
	}
}
