using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Dependency;
using StockPilot.Authorization.Users.Profile.Dto;
using StockPilot.Services.Common.Dtos;

namespace StockPilot.Authorization.Users.Profile
{
	public interface IProfileAppService : IApplicationService, ITransientDependency
	{
		Task<CurrentUserProfileEditDto> GetCurrentUserProfileForEdit();

		Task UpdateCurrentUserProfile(CurrentUserProfileEditDto input);

		Task ChangePassword(ChangePasswordInput input);

		Task UpdateProfilePicture(UpdateProfilePictureInput input);

		Task<PictureDto> GetProfilePicture();

		Task<PictureDto> UploadProfilePicture(PictureContentDto input);

		Task DeleteProfilePicture();

		Task<GetPasswordComplexitySettingOutput> GetPasswordComplexitySetting();
	}
}
