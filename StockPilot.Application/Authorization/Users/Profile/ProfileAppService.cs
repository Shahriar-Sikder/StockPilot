using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Authorization;
using Abp.AutoMapper;
using Abp.Configuration;
using Abp.Dependency;
using Abp.Extensions;
using Abp.IO;
using Abp.Runtime.Session;
using Abp.Timing;
using Abp.UI;
using Newtonsoft.Json;
using StockPilot.Authorization.Users.Profile.Dto;
using StockPilot.Security;
using StockPilot.Services.Common.Dtos;
using StockPilot.Storage;
using StockPilot.Timing;

namespace StockPilot.Authorization.Users.Profile
{
	[AbpAuthorize(new string[] { })]
	public class ProfileAppService : StockPilotAppServiceBase, IProfileAppService, IApplicationService, ITransientDependency
	{
		private readonly IAppFolders _appFolders;

		private readonly IBinaryObjectManager _binaryObjectManager;

		private readonly ITimeZoneService _timeZoneService;

		public const int MaxProfilePictureBytes = 204800;

		public ProfileAppService(IAppFolders appFolders, IBinaryObjectManager binaryObjectManager, ITimeZoneService timezoneService)
		{
			_appFolders = appFolders;
			_binaryObjectManager = binaryObjectManager;
			_timeZoneService = timezoneService;
		}

		public async Task<CurrentUserProfileEditDto> GetCurrentUserProfileForEdit()
		{
			CurrentUserProfileEditDto userProfileEditDto = (await GetCurrentUserAsync()).MapTo<CurrentUserProfileEditDto>();
			if (Clock.SupportsMultipleTimezone)
			{
				CurrentUserProfileEditDto currentUserProfileEditDto = userProfileEditDto;
				currentUserProfileEditDto.Timezone = await base.SettingManager.GetSettingValueAsync("Abp.Timing.TimeZone");
				string defaultTimeZoneId = await _timeZoneService.GetDefaultTimezoneAsync(SettingScopes.User, base.AbpSession.TenantId);
				if (userProfileEditDto.Timezone == defaultTimeZoneId)
				{
					userProfileEditDto.Timezone = string.Empty;
				}
			}
			return userProfileEditDto;
		}

		public async Task UpdateCurrentUserProfile(CurrentUserProfileEditDto input)
		{
			User user = await GetCurrentUserAsync();
			input.MapTo(user);
			CheckErrors(await base.UserManager.UpdateAsync(user));
			if (Clock.SupportsMultipleTimezone)
			{
				if (input.Timezone.IsNullOrEmpty())
				{
					string defaultValue = await _timeZoneService.GetDefaultTimezoneAsync(SettingScopes.User, base.AbpSession.TenantId);
					await base.SettingManager.ChangeSettingForUserAsync(base.AbpSession.ToUserIdentifier(), "Abp.Timing.TimeZone", defaultValue);
				}
				else
				{
					await base.SettingManager.ChangeSettingForUserAsync(base.AbpSession.ToUserIdentifier(), "Abp.Timing.TimeZone", input.Timezone);
				}
			}
		}

		public async Task ChangePassword(ChangePasswordInput input)
		{
			await CheckPasswordComplexity(input.NewPassword);
			User user = await GetCurrentUserAsync();
			CheckErrors(await base.UserManager.ChangePasswordAsync(user.Id, input.CurrentPassword, input.NewPassword));
		}

		public async Task UpdateProfilePicture(UpdateProfilePictureInput input)
		{
			string tempProfilePicturePath = Path.Combine(_appFolders.TempFileDownloadFolder, input.FileName);
			byte[] byteArray;
			using (FileStream fsTempProfilePicture = new FileStream(tempProfilePicturePath, FileMode.Open))
			{
				using (Bitmap bmpImage = new Bitmap(fsTempProfilePicture))
				{
					int width = ((input.Width == 0) ? bmpImage.Width : input.Width);
					int height = ((input.Height == 0) ? bmpImage.Height : input.Height);
					Bitmap bmCrop = bmpImage.Clone(new Rectangle(input.X, input.Y, width, height), bmpImage.PixelFormat);
					using (MemoryStream stream = new MemoryStream())
					{
						bmCrop.Save(stream, bmpImage.RawFormat);
						stream.Close();
						byteArray = stream.ToArray();
					}
				}
			}
			if (byteArray.LongLength > 102400)
			{
				throw new UserFriendlyException(L("ResizedProfilePicture_Warn_SizeLimit"));
			}
			User user = await base.UserManager.GetUserByIdAsync(base.AbpSession.GetUserId());
			if (user.ProfilePictureId.HasValue)
			{
				await _binaryObjectManager.DeleteAsync(user.ProfilePictureId.Value);
			}
			BinaryObject storedFile = new BinaryObject(base.AbpSession.TenantId, byteArray);
			await _binaryObjectManager.SaveAsync(storedFile);
			user.ProfilePictureId = storedFile.Id;
			FileHelper.DeleteIfExists(tempProfilePicturePath);
		}

		public async Task<PictureDto> GetProfilePicture()
		{
			User user = await GetCurrentUserAsync();
			if (!user.ProfilePictureId.HasValue)
			{
				return null;
			}
			BinaryObject file = await _binaryObjectManager.GetOrNullAsync(user.ProfilePictureId.Value);
			return (file == null) ? null : new PictureDto
			{
				ContentType = ImageContentType(file.Bytes),
				Base64Content = Convert.ToBase64String(file.Bytes)
			};
		}

		public async Task<PictureDto> UploadProfilePicture(PictureContentDto input)
		{
			string content = (input?.Base64Content ?? string.Empty).Trim();
			int comma = content.IndexOf(',');
			if (content.StartsWith("data:") && comma > 0)
			{
				content = content.Substring(comma + 1);
			}
			byte[] bytes;
			try
			{
				bytes = Convert.FromBase64String(content);
			}
			catch (FormatException)
			{
				throw new UserFriendlyException(L("ProfilePicture_NotAnImage"));
			}
			if (bytes.Length == 0)
			{
				throw new UserFriendlyException(L("ProfilePicture_NotAnImage"));
			}
			if (bytes.Length > 204800)
			{
				throw new UserFriendlyException(L("ProfilePicture_TooLarge"));
			}
			try
			{
				using (MemoryStream stream = new MemoryStream(bytes))
				{
					using (Image image = Image.FromStream(stream))
					{
						if (image.Width < 16 || image.Height < 16)
						{
							throw new UserFriendlyException(L("ProfilePicture_NotAnImage"));
						}
					}
				}
			}
			catch (ArgumentException)
			{
				throw new UserFriendlyException(L("ProfilePicture_NotAnImage"));
			}
			User user = await GetCurrentUserAsync();
			if (user.ProfilePictureId.HasValue)
			{
				await _binaryObjectManager.DeleteAsync(user.ProfilePictureId.Value);
			}
			BinaryObject storedFile = new BinaryObject(base.AbpSession.TenantId, bytes);
			await _binaryObjectManager.SaveAsync(storedFile);
			user.ProfilePictureId = storedFile.Id;
			return new PictureDto
			{
				ContentType = ImageContentType(bytes),
				Base64Content = Convert.ToBase64String(bytes)
			};
		}

		public async Task DeleteProfilePicture()
		{
			User user = await GetCurrentUserAsync();
			if (user.ProfilePictureId.HasValue)
			{
				await _binaryObjectManager.DeleteAsync(user.ProfilePictureId.Value);
				user.ProfilePictureId = null;
			}
		}

		private static string ImageContentType(byte[] bytes)
		{
			if (bytes.Length > 3 && bytes[0] == 137 && bytes[1] == 80)
			{
				return "image/png";
			}
			if (bytes.Length > 3 && bytes[0] == 71 && bytes[1] == 73)
			{
				return "image/gif";
			}
			if (bytes.Length > 11 && bytes[8] == 87 && bytes[9] == 69 && bytes[10] == 66 && bytes[11] == 80)
			{
				return "image/webp";
			}
			return "image/jpeg";
		}

		public async Task<GetPasswordComplexitySettingOutput> GetPasswordComplexitySetting()
		{
			PasswordComplexitySetting setting = JsonConvert.DeserializeObject<PasswordComplexitySetting>(await base.SettingManager.GetSettingValueAsync("App.Security.PasswordComplexity"));
			return new GetPasswordComplexitySettingOutput
			{
				Setting = setting
			};
		}

		private async Task CheckPasswordComplexity(string password)
		{
			PasswordComplexitySetting passwordComplexitySetting = JsonConvert.DeserializeObject<PasswordComplexitySetting>(await base.SettingManager.GetSettingValueAsync("App.Security.PasswordComplexity"));
			if (!new PasswordComplexityChecker().Check(passwordComplexitySetting, password))
			{
				throw new UserFriendlyException(L("PasswordComplexityNotSatisfied"));
			}
		}
	}
}
