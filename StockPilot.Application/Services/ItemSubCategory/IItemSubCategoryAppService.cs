using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Dependency;
using StockPilot.Dto;
using StockPilot.Services.Common.Dtos;
using StockPilot.Services.ItemSubCategory.Dtos;

namespace StockPilot.Services.ItemSubCategory
{
	public interface IItemSubCategoryAppService : IApplicationService, ITransientDependency
	{
		Task<PagedResultDto<ItemSubCategoryListDto>> GetAll(ItemSubCategoryGridInput input);

		Task<FileDto> GetAllToExcel(ItemSubCategoryGridInput input);

		Task<EditItemSubCategoryDto> GetForEdit(EntityDto input);

		Task<EntityDto> Create(CreateItemSubCategoryDto input);

		Task Update(EditItemSubCategoryDto input);

		Task Delete(EntityDto input);

		Task Restore(EntityDto input);

		Task<ListResultDto<ItemSubCategoryComboDto>> GetForCombo();

		Task<PictureDto> UploadPicture(UploadPictureInput input);

		Task<PictureDto> GetPicture(EntityDto input);

		Task DeletePicture(EntityDto input);
	}
}
