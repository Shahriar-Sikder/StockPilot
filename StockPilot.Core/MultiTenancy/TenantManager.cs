using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Abp;
using Abp.Application.Editions;
using Abp.Application.Features;
using Abp.Configuration;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.IdentityFramework;
using Abp.MultiTenancy;
using Abp.Notifications;
using Abp.Runtime.Security;
using Abp.UI;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using StockPilot.Authorization.Roles;
using StockPilot.Authorization.Users;
using StockPilot.Editions;
using StockPilot.Notifications;
using StockPilot.Security;

namespace StockPilot.MultiTenancy
{
	public class TenantManager : AbpTenantManager<Tenant, User>
	{
		private readonly IUnitOfWorkManager _unitOfWorkManager;

		private readonly RoleManager _roleManager;

		private readonly UserManager _userManager;

		private readonly IUserEmailer _userEmailer;

		private readonly INotificationSubscriptionManager _notificationSubscriptionManager;

		private readonly IAppNotifier _appNotifier;

		private readonly ISettingManager _settingManager;

		public TenantManager(IRepository<Tenant> tenantRepository, IRepository<TenantFeatureSetting, long> tenantFeatureRepository, EditionManager editionManager, IUnitOfWorkManager unitOfWorkManager, RoleManager roleManager, IUserEmailer userEmailer, UserManager userManager, INotificationSubscriptionManager notificationSubscriptionManager, IAppNotifier appNotifier, IAbpZeroFeatureValueStore featureValueStore, ISettingManager settingManager)
			: base(tenantRepository, tenantFeatureRepository, (AbpEditionManager)editionManager, featureValueStore)
		{
			_unitOfWorkManager = unitOfWorkManager;
			_roleManager = roleManager;
			_userEmailer = userEmailer;
			_userManager = userManager;
			_notificationSubscriptionManager = notificationSubscriptionManager;
			_appNotifier = appNotifier;
			_settingManager = settingManager;
		}

		public async Task<int> CreateWithAdminUserAsync(string tenancyName, string name, string adminPassword, string adminEmailAddress, string connectionString, bool isActive, int? editionId, bool shouldChangePasswordOnNextLogin, bool sendActivationEmail, string adminPhoneNumber = null, string adminName = null, string adminSurname = null)
		{
			int newTenantId;
			long newAdminId;
			using (IUnitOfWorkCompleteHandle uow = _unitOfWorkManager.Begin(TransactionScopeOption.RequiresNew))
			{
				Tenant tenant = new Tenant(tenancyName, name)
				{
					IsActive = isActive,
					EditionId = editionId,
					ConnectionString = (connectionString.IsNullOrWhiteSpace() ? null : SimpleStringCipher.Instance.Encrypt(connectionString))
				};
				await CreateAsync(tenant);
				await _unitOfWorkManager.Current.SaveChangesAsync();
				using (_unitOfWorkManager.Current.SetTenantId(tenant.Id))
				{
					await EnsureTenantLanguageAsync(tenant.Id);
					CheckErrors(await _roleManager.CreateStaticRoles(tenant.Id));
					await _unitOfWorkManager.Current.SaveChangesAsync();
					Role adminRole = _roleManager.Roles.Single((Role r) => r.Name == "Admin");
					await _roleManager.GrantAllPermissionsAsync(adminRole);
					Role userRole = _roleManager.Roles.Single((Role r) => r.Name == "User");
					userRole.IsDefault = true;
					CheckErrors(await _roleManager.UpdateAsync(userRole));
					bool num = !adminPassword.IsNullOrEmpty();
					if (!num)
					{
						adminPassword = User.CreateRandomPassword();
					}
					if (num)
					{
						PasswordComplexitySetting passwordComplexity = JsonConvert.DeserializeObject<PasswordComplexitySetting>(await _settingManager.GetSettingValueForApplicationAsync("App.Security.PasswordComplexity"));
						if (passwordComplexity == null || !new PasswordComplexityChecker().Check(passwordComplexity, adminPassword))
						{
							throw new UserFriendlyException(base.LocalizationManager.GetSource("StockPilot").GetString("PasswordComplexityNotSatisfied"));
						}
					}
					User adminUser = User.CreateTenantAdminUser(tenant.Id, adminEmailAddress, adminPassword, adminPhoneNumber, adminName, adminSurname);
					adminUser.ShouldChangePasswordOnNextLogin = shouldChangePasswordOnNextLogin;
					adminUser.IsActive = true;
					CheckErrors(await _userManager.CreateAsync(adminUser));
					await _unitOfWorkManager.Current.SaveChangesAsync();
					CheckErrors(await _userManager.AddToRoleAsync(adminUser.Id, adminRole.Name));
					await _appNotifier.WelcomeToTheApplicationAsync(adminUser);
					if (sendActivationEmail)
					{
						adminUser.SetNewEmailConfirmationCode();
						await _userEmailer.SendEmailActivationLinkAsync(adminUser, adminPassword);
					}
					await _unitOfWorkManager.Current.SaveChangesAsync();
					newTenantId = tenant.Id;
					newAdminId = adminUser.Id;
				}
				await uow.CompleteAsync();
			}
			using (IUnitOfWorkCompleteHandle uow = _unitOfWorkManager.Begin(TransactionScopeOption.RequiresNew))
			{
				using (_unitOfWorkManager.Current.SetTenantId(newTenantId))
				{
					await _notificationSubscriptionManager.SubscribeToAllAvailableNotificationsAsync(new UserIdentifier(newTenantId, newAdminId));
					await _unitOfWorkManager.Current.SaveChangesAsync();
					await uow.CompleteAsync();
				}
			}
			return newTenantId;
		}

		private async Task EnsureTenantLanguageAsync(int tenantId)
		{
			if ((await _settingManager.GetSettingValueForTenantAsync("Abp.Localization.DefaultLanguageName", tenantId)).IsNullOrWhiteSpace())
			{
				await _settingManager.ChangeSettingForTenantAsync(tenantId, "Abp.Localization.DefaultLanguageName", "en");
			}
		}

		public virtual async Task<Tenant> CreateWithAdminPasswordHashAsync(string tenancyName, string name, string adminPasswordHash, string adminEmailAddress, bool isActive, int? editionId, string adminPhoneNumber, string adminName, string adminSurname)
		{
			IActiveUnitOfWork uow = _unitOfWorkManager.Current;
			if (uow == null)
			{
				throw new AbpException("CreateWithAdminPasswordHashAsync must run inside a unit of work.");
			}
			if (adminPasswordHash.IsNullOrWhiteSpace())
			{
				throw new AbpException("The administrator's password hash is missing.");
			}
			Tenant tenant = new Tenant(tenancyName, name)
			{
				IsActive = isActive,
				EditionId = editionId
			};
			await CreateAsync(tenant);
			await uow.SaveChangesAsync();
			using (uow.SetTenantId(tenant.Id))
			{
				await EnsureTenantLanguageAsync(tenant.Id);
				CheckErrors(await _roleManager.CreateStaticRoles(tenant.Id));
				await uow.SaveChangesAsync();
				Role adminRole = _roleManager.Roles.Single((Role r) => r.Name == "Admin");
				await _roleManager.GrantAllPermissionsAsync(adminRole);
				Role userRole = _roleManager.Roles.Single((Role r) => r.Name == "User");
				userRole.IsDefault = true;
				CheckErrors(await _roleManager.UpdateAsync(userRole));
				User adminUser = User.CreateTenantAdminUser(tenant.Id, adminEmailAddress, User.CreateRandomPassword(), adminPhoneNumber, adminName, adminSurname);
				adminUser.Password = adminPasswordHash;
				adminUser.IsActive = true;
				adminUser.IsEmailConfirmed = true;
				CheckErrors(await _userManager.CreateAsync(adminUser));
				await uow.SaveChangesAsync();
				CheckErrors(await _userManager.AddToRoleAsync(adminUser.Id, adminRole.Name));
				await _appNotifier.WelcomeToTheApplicationAsync(adminUser);
				await uow.SaveChangesAsync();
				await _notificationSubscriptionManager.SubscribeToAllAvailableNotificationsAsync(new UserIdentifier(tenant.Id, adminUser.Id));
				await uow.SaveChangesAsync();
			}
			return tenant;
		}

		protected virtual void CheckErrors(IdentityResult identityResult)
		{
			identityResult.CheckErrors(base.LocalizationManager);
		}
	}
}
