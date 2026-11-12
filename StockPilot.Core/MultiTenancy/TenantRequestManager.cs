using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.UI;
using Newtonsoft.Json;
using StockPilot.Authorization.Users;
using StockPilot.Security;

namespace StockPilot.MultiTenancy
{
	public class TenantRequestManager : StockPilotDomainServiceBase
	{
		public const string RequestNoPrefix = "TR-";

		public const int RequestNoDigits = 5;

		private const int SaveAttempts = 3;

		private readonly IRepository<TenantRequest> _requestRepository;

		private readonly IRepository<Tenant> _tenantRepository;

		private readonly UserManager _userManager;

		public TenantRequestManager(IRepository<TenantRequest> requestRepository, IRepository<Tenant> tenantRepository, UserManager userManager)
		{
			_requestRepository = requestRepository;
			_tenantRepository = tenantRepository;
			_userManager = userManager;
		}

		public virtual async Task<TenantRequest> CreateAsync(TenantRequest request, string plainPassword)
		{
			request.TenancyName = request.TenancyName?.Trim();
			await CheckTenancyNameIsFreeAsync(request.TenancyName, null);
			await CheckPasswordComplexityAsync(plainPassword);
			request.Status = TenantRequestStatus.Pending;
			request.AdminPasswordHash = _userManager.PasswordHasher.HashPassword(plainPassword);
			TenantRequest tenantRequest = request;
			tenantRequest.RequestNo = await NextRequestNoAsync();
			await _requestRepository.InsertAsync(request);
			int attempt = 1;
			while (true)
			{
				try
				{
					await base.CurrentUnitOfWork.SaveChangesAsync();
					return request;
				}
				catch (Exception ex) when (attempt < 3 && IsRequestNoClash(ex))
				{
					tenantRequest = request;
					tenantRequest.RequestNo = await NextRequestNoAsync(attempt);
				}
				attempt++;
			}
		}

		public virtual async Task CheckTenancyNameIsFreeAsync(string tenancyName, int? exceptRequestId)
		{
			if (tenancyName.IsNullOrWhiteSpace() || !Regex.IsMatch(tenancyName, "^[a-zA-Z][a-zA-Z0-9_-]{1,}$"))
			{
				throw new UserFriendlyException(L("InvalidTenancyName"));
			}
			string upper = tenancyName.ToUpperInvariant();
			using (base.CurrentUnitOfWork.SetTenantId(null))
			{
				if (await _tenantRepository.CountAsync((Tenant t) => t.TenancyName.ToUpper() == upper) > 0)
				{
					throw new UserFriendlyException(L("TenancyNameIsAlreadyTaken", tenancyName));
				}
				if (await _requestRepository.CountAsync((TenantRequest r) => (int)r.Status == 0 && r.TenancyName.ToUpper() == upper && (exceptRequestId == (int?)null || (int?)r.Id != exceptRequestId)) > 0)
				{
					throw new UserFriendlyException(L("TenancyNameIsAlreadyRequested", tenancyName));
				}
			}
		}

		public virtual async Task CheckPasswordComplexityAsync(string plainPassword)
		{
			PasswordComplexitySetting complexity = JsonConvert.DeserializeObject<PasswordComplexitySetting>(await base.SettingManager.GetSettingValueForApplicationAsync("App.Security.PasswordComplexity"));
			if (plainPassword.IsNullOrEmpty() || complexity == null || !new PasswordComplexityChecker().Check(complexity, plainPassword))
			{
				throw new UserFriendlyException(L("PasswordComplexityNotSatisfied"));
			}
		}

		public virtual async Task<string> NextRequestNoAsync(int skip = 0)
		{
			using (base.CurrentUnitOfWork.DisableFilter("SoftDelete"))
			{
				using (base.CurrentUnitOfWork.SetTenantId(null))
				{
					int result;
					int last = (from n in (await _requestRepository.GetAllListAsync((TenantRequest r) => r.RequestNo.StartsWith("TR-"))).Select((TenantRequest r) => r.RequestNo).ToList()
						select int.TryParse(n.Substring("TR-".Length), NumberStyles.None, CultureInfo.InvariantCulture, out result) ? result : 0).DefaultIfEmpty(0).Max();
					return "TR-" + (last + 1 + skip).ToString("D" + 5, CultureInfo.InvariantCulture);
				}
			}
		}

		private static bool IsRequestNoClash(Exception ex)
		{
			for (Exception inner = ex; inner != null; inner = inner.InnerException)
			{
				if (inner.Message.Contains("UX_TenantRequests_RequestNo"))
				{
					return true;
				}
			}
			return false;
		}
	}
}
