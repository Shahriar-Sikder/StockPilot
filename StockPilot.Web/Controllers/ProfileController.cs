using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Abp.Auditing;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.IO.Extensions;
using Abp.Runtime.Session;
using Abp.UI;
using Abp.Web.Models;
using Abp.Web.Mvc.Authorization;
using StockPilot.Authorization.Users;
using StockPilot.IO;
using StockPilot.Storage;

namespace StockPilot.Web.Controllers
{
	[AbpMvcAuthorize(new string[] { })]
	public class ProfileController : StockPilotControllerBase
	{
		private readonly UserManager _userManager;

		private readonly IBinaryObjectManager _binaryObjectManager;

		private readonly IAppFolders _appFolders;

		public ProfileController(UserManager userManager, IBinaryObjectManager binaryObjectManager, IAppFolders appFolders)
		{
			_userManager = userManager;
			_binaryObjectManager = binaryObjectManager;
			_appFolders = appFolders;
		}

		[DisableAuditing]
		public async Task<FileResult> GetProfilePicture()
		{
			User user = await _userManager.GetUserByIdAsync(base.AbpSession.GetUserId());
			if (!user.ProfilePictureId.HasValue)
			{
				return GetDefaultProfilePicture();
			}
			return await GetProfilePictureById(user.ProfilePictureId.Value);
		}

		[DisableAuditing]
		public async Task<FileResult> GetProfilePictureById(string id = "")
		{
			if (id.IsNullOrEmpty())
			{
				return GetDefaultProfilePicture();
			}
			return await GetProfilePictureById(Guid.Parse(id));
		}

		[UnitOfWork]
		public virtual async Task<JsonResult> ChangeProfilePicture()
		{
			_ = 2;
			try
			{
				if (base.Request.Files.Count <= 0 || base.Request.Files[0] == null)
				{
					throw new UserFriendlyException(L("ProfilePicture_Change_Error"));
				}
				HttpPostedFileBase file = base.Request.Files[0];
				if (file.ContentLength > 30720)
				{
					throw new UserFriendlyException(L("ProfilePicture_Warn_SizeLimit"));
				}
				User user = await _userManager.GetUserByIdAsync(base.AbpSession.GetUserId());
				if (user.ProfilePictureId.HasValue)
				{
					await _binaryObjectManager.DeleteAsync(user.ProfilePictureId.Value);
				}
				BinaryObject storedFile = new BinaryObject(base.AbpSession.TenantId, file.InputStream.GetAllBytes());
				await _binaryObjectManager.SaveAsync(storedFile);
				user.ProfilePictureId = storedFile.Id;
				return Json(new AjaxResponse());
			}
			catch (UserFriendlyException ex)
			{
				return Json(new AjaxResponse(new ErrorInfo(ex.Message)));
			}
		}

		public JsonResult UploadProfilePicture()
		{
			try
			{
				if (base.Request.Files.Count <= 0 || base.Request.Files[0] == null)
				{
					throw new UserFriendlyException(L("ProfilePicture_Change_Error"));
				}
				HttpPostedFileBase httpPostedFileBase = base.Request.Files[0];
				if (httpPostedFileBase.ContentLength > 1048576)
				{
					throw new UserFriendlyException(L("ProfilePicture_Warn_SizeLimit"));
				}
				Image fileImage = Image.FromStream(httpPostedFileBase.InputStream);
				if (!new List<ImageFormat>
				{
					ImageFormat.Jpeg,
					ImageFormat.Png,
					ImageFormat.Gif
				}.Contains(fileImage.RawFormat))
				{
					throw new ApplicationException("Uploaded file is not an accepted image file !");
				}
				AppFileHelper.DeleteFilesInFolderIfExists(_appFolders.TempFileDownloadFolder, "userProfileImage_" + base.AbpSession.GetUserId());
				string tempFileName = string.Concat(str2: new FileInfo(httpPostedFileBase.FileName).Extension, str0: "userProfileImage_", str1: base.AbpSession.GetUserId().ToString());
				string tempFilePath = Path.Combine(_appFolders.TempFileDownloadFolder, tempFileName);
				httpPostedFileBase.SaveAs(tempFilePath);
				using (Bitmap bmpImage = new Bitmap(tempFilePath))
				{
					return Json(new AjaxResponse(new
					{
						fileName = tempFileName,
						width = bmpImage.Width,
						height = bmpImage.Height
					}));
				}
			}
			catch (UserFriendlyException ex)
			{
				return Json(new AjaxResponse(new ErrorInfo(ex.Message)));
			}
		}

		private FileResult GetDefaultProfilePicture()
		{
			return File(base.Server.MapPath("~/Common/Images/default-profile-picture.png"), "image/png");
		}

		private async Task<FileResult> GetProfilePictureById(Guid profilePictureId)
		{
			BinaryObject file = await _binaryObjectManager.GetOrNullAsync(profilePictureId);
			if (file == null)
			{
				return GetDefaultProfilePicture();
			}
			return File(file.Bytes, "image/jpeg");
		}
	}
}
