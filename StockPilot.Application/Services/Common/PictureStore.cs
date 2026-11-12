using System;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Runtime.Session;
using Abp.UI;
using StockPilot.Services.Common.Dtos;
using StockPilot.Storage;

namespace StockPilot.Services.Common
{
	public class PictureStore : StockPilotServiceBase, ITransientDependency
	{
		private readonly IBinaryObjectManager _binaryObjectManager;

		public IAbpSession AbpSession { get; set; }

		public PictureStore(IBinaryObjectManager binaryObjectManager)
		{
			_binaryObjectManager = binaryObjectManager;
			AbpSession = NullAbpSession.Instance;
		}

		public Task<Guid> ReplaceAsync(Guid? previousPictureId, UploadPictureInput input)
		{
			return ReplaceAsync(previousPictureId, input.Base64Content);
		}

		public Task<Guid> ReplaceAsync(Guid? previousPictureId, PictureContentDto input)
		{
			return ReplaceAsync(previousPictureId, input.Base64Content);
		}

		private async Task<Guid> ReplaceAsync(Guid? previousPictureId, string base64Content)
		{
			byte[] bytes = PictureHelper.DecodeBase64(base64Content);
			if (bytes == null || bytes.Length == 0)
			{
				throw new UserFriendlyException(L("InvalidPictureFile"));
			}
			if (bytes.Length > 3145728)
			{
				throw new UserFriendlyException(L("PictureTooLarge", 3));
			}
			byte[] resized = PictureHelper.ResizeToPng(bytes);
			if (resized == null)
			{
				throw new UserFriendlyException(L("InvalidPictureFile"));
			}
			BinaryObject stored = new BinaryObject(AbpSession.TenantId, resized);
			await _binaryObjectManager.SaveAsync(stored);
			if (previousPictureId.HasValue)
			{
				await _binaryObjectManager.DeleteAsync(previousPictureId.Value);
			}
			return stored.Id;
		}

		public async Task<PictureDto> GetOrNullAsync(Guid? pictureId)
		{
			if (!pictureId.HasValue)
			{
				return null;
			}
			BinaryObject file = await _binaryObjectManager.GetOrNullAsync(pictureId.Value);
			return (file == null) ? null : new PictureDto
			{
				ContentType = "image/png",
				Base64Content = Convert.ToBase64String(file.Bytes)
			};
		}

		public async Task DeleteAsync(Guid? pictureId)
		{
			if (pictureId.HasValue)
			{
				await _binaryObjectManager.DeleteAsync(pictureId.Value);
			}
		}
	}
}
