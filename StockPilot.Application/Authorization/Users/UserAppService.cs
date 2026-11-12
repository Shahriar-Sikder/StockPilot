using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Linq.Dynamic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Authorization.Roles;
using Abp.Authorization.Users;
using Abp.AutoMapper;
using Abp.Configuration;
using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.Notifications;
using Abp.Runtime.Session;
using Abp.UI;
using Microsoft.AspNet.Identity;
using StockPilot.Authorization.Permissions;
using StockPilot.Authorization.Permissions.Dto;
using StockPilot.Authorization.Roles;
using StockPilot.Authorization.Users.Dto;
using StockPilot.Authorization.Users.Exporting;
using StockPilot.Dto;
using StockPilot.Notifications;

namespace StockPilot.Authorization.Users
{
	[AbpAuthorize(new string[] { "Pages.Administration.Users" })]
	public class UserAppService : StockPilotAppServiceBase, IUserAppService, IApplicationService, ITransientDependency
	{
		private readonly RoleManager _roleManager;

		private readonly IUserEmailer _userEmailer;

		private readonly IUserListExcelExporter _userListExcelExporter;

		private readonly INotificationSubscriptionManager _notificationSubscriptionManager;

		private readonly IAppNotifier _appNotifier;

		private readonly IRepository<RolePermissionSetting, long> _rolePermissionRepository;

		private readonly IRepository<UserPermissionSetting, long> _userPermissionRepository;

		private readonly IRepository<UserRole, long> _userRoleRepository;

		private readonly IUserPolicy _userPolicy;

		private readonly UserAccessPolicy _access;

		public UserAppService(RoleManager roleManager, IUserEmailer userEmailer, IUserListExcelExporter userListExcelExporter, INotificationSubscriptionManager notificationSubscriptionManager, IAppNotifier appNotifier, IRepository<RolePermissionSetting, long> rolePermissionRepository, IRepository<UserPermissionSetting, long> userPermissionRepository, IRepository<UserRole, long> userRoleRepository, IUserPolicy userPolicy, UserAccessPolicy access)
		{
			_access = access;
			_roleManager = roleManager;
			_userEmailer = userEmailer;
			_userListExcelExporter = userListExcelExporter;
			_notificationSubscriptionManager = notificationSubscriptionManager;
			_appNotifier = appNotifier;
			_rolePermissionRepository = rolePermissionRepository;
			_userPermissionRepository = userPermissionRepository;
			_userRoleRepository = userRoleRepository;
			_userPolicy = userPolicy;
		}

		public async Task<PagedResultDto<UserListDto>> GetUsers(GetUsersInput input)
		{
			int? tenantId = base.AbpSession.TenantId;
			List<long> hidden = await HiddenUserIdsAsync();
			IQueryable<User> query = base.UserManager.Users.Where((User u) => u.TenantId == tenantId).WhereIf(hidden.Count > 0, (User u) => !hidden.Contains(u.Id)).Include((User u) => u.Roles)
				.WhereIf(input.Role.HasValue, (User u) => u.Roles.Any((UserRole r) => r.RoleId == input.Role.Value))
				.WhereIf(!input.Filter.IsNullOrWhiteSpace(), (User u) => u.Name.Contains(input.Filter) || u.Surname.Contains(input.Filter) || u.UserName.Contains(input.Filter) || u.EmailAddress.Contains(input.Filter));
			if (!input.Permission.IsNullOrWhiteSpace())
			{
				query = from user in query
					join ur in _userRoleRepository.GetAll() on user.Id equals ur.UserId into urJoined
					from ur in urJoined.DefaultIfEmpty()
					join up in _userPermissionRepository.GetAll() on new
					{
						UserId = user.Id,
						Name = input.Permission
					} equals new { up.UserId, up.Name } into upJoined
					from up in upJoined.DefaultIfEmpty()
					join rp in _rolePermissionRepository.GetAll() on new
					{
						RoleId = ur.RoleId,
						Name = input.Permission
					} equals new { rp.RoleId, rp.Name } into rpJoined
					from rp in rpJoined.DefaultIfEmpty()
					where (up != null && up.IsGranted) || (up == null && rp != null)
					group user by user into userGrouped
					select userGrouped.Key;
			}
			int userCount = await query.CountAsync();
			List<UserListDto> userListDtos = (await query.OrderBy(input.Sorting).PageBy(input).ToListAsync()).MapTo<List<UserListDto>>();
			await FillRoleNames(userListDtos);
			return new PagedResultDto<UserListDto>(userCount, userListDtos);
		}

		public async Task<FileDto> GetUsersToExcel()
		{
			int? tenantId = base.AbpSession.TenantId;
			List<long> hidden = await HiddenUserIdsAsync();
			List<UserListDto> userListDtos = (await base.UserManager.Users.Where((User u) => u.TenantId == tenantId).WhereIf(hidden.Count > 0, (User u) => !hidden.Contains(u.Id)).Include((User u) => u.Roles)
				.ToListAsync()).MapTo<List<UserListDto>>();
			await FillRoleNames(userListDtos);
			return _userListExcelExporter.ExportToFile(userListDtos);
		}

		[AbpAuthorize(new string[] { "Pages.Administration.Users.Create", "Pages.Administration.Users.Edit" })]
		public async Task<GetUserForEditOutput> GetUserForEdit(NullableIdDto<long> input)
		{
			UserRoleDto[] userRoleDtos = await (from r in MyRoles()
				orderby r.DisplayName
				select new UserRoleDto
				{
					RoleId = r.Id,
					RoleName = r.Name,
					RoleDisplayName = r.DisplayName
				}).ToArrayAsync();
			HashSet<string> grantable = await _access.GetGrantablePermissionNamesAsync();
			UserRoleDto[] array = userRoleDtos;
			foreach (UserRoleDto dto in array)
			{
				UserRoleDto userRoleDto = dto;
				UserAccessPolicy access = _access;
				userRoleDto.CanChange = await access.CanChangeRoleAsync(await _roleManager.GetRoleByIdAsync(dto.RoleId), grantable);
			}
			GetUserForEditOutput output = new GetUserForEditOutput
			{
				Roles = userRoleDtos
			};
			if (!input.Id.HasValue)
			{
				GetUserForEditOutput getUserForEditOutput = output;
				UserEditDto userEditDto = new UserEditDto
				{
					IsActive = true,
					ShouldChangePasswordOnNextLogin = true
				};
				UserEditDto userEditDto2 = userEditDto;
				userEditDto2.IsTwoFactorEnabled = await base.SettingManager.GetSettingValueAsync<bool>("Abp.Zero.UserManagement.TwoFactorLogin.IsEnabled");
				UserEditDto userEditDto3 = userEditDto;
				userEditDto3.IsLockoutEnabled = await base.SettingManager.GetSettingValueAsync<bool>("Abp.Zero.UserManagement.UserLockOut.IsEnabled");
				getUserForEditOutput.User = userEditDto;
				foreach (Role defaultRole in await (from r in MyRoles()
					where r.IsDefault
					select r).ToListAsync())
				{
					UserRoleDto defaultUserRole = userRoleDtos.FirstOrDefault((UserRoleDto ur) => ur.RoleName == defaultRole.Name);
					if (defaultUserRole != null)
					{
						defaultUserRole.IsAssigned = true;
					}
				}
			}
			else
			{
				User user = await GetMyUserAsync(input.Id.Value);
				output.User = user.MapTo<UserEditDto>();
				output.IsOwnAccount = user.Id == base.AbpSession.GetUserId() && grantable != null;
				output.ProfilePictureId = user.ProfilePictureId;
				long userId = user.Id;
				List<int> assignedRoleIds = (await _userRoleRepository.GetAllListAsync((UserRole ur) => ur.UserId == userId)).Select((UserRole ur) => ur.RoleId).ToList();
				UserRoleDto[] array2 = userRoleDtos;
				foreach (UserRoleDto userRoleDto2 in array2)
				{
					userRoleDto2.IsAssigned = assignedRoleIds.Contains(userRoleDto2.RoleId);
				}
			}
			return output;
		}

		[AbpAuthorize(new string[] { "Pages.Administration.Users.ChangePermissions" })]
		public async Task<GetUserPermissionsForEditOutput> GetUserPermissionsForEdit(EntityDto<long> input)
		{
			User user = await GetMyUserAsync(input.Id);
			IReadOnlyList<Permission> permissions = base.PermissionManager.GetAllPermissions();
			List<string> grantedPermissionNames = (await base.UserManager.GetGrantedPermissionsAsync(user)).Select((Permission p) => p.Name).ToList();
			HashSet<string> rolePermissionNames = new HashSet<string>();
			foreach (string roleName in await base.UserManager.GetRolesAsync(user.Id))
			{
				Role role = await _roleManager.FindByNameAsync(roleName);
				if (role != null)
				{
					HashSet<string> hashSet = rolePermissionNames;
					hashSet.UnionWith((await _roleManager.GetGrantedPermissionsAsync(role)).Select((Permission p) => p.Name));
				}
			}
			HashSet<string> grantable = await _access.GetGrantablePermissionNamesAsync();
			GetUserPermissionsForEditOutput getUserPermissionsForEditOutput = new GetUserPermissionsForEditOutput
			{
				RolePermissionNames = rolePermissionNames.ToList(),
				GrantablePermissionNames = grantable?.ToList(),
				IsOwnAccount = (user.Id == base.AbpSession.GetUserId() && grantable != null)
			};
			GetUserPermissionsForEditOutput getUserPermissionsForEditOutput2 = getUserPermissionsForEditOutput;
			getUserPermissionsForEditOutput2.Permissions = await PermissionFeatureFilter.RemoveDisabledFeaturePermissions(permissions.MapTo<List<FlatPermissionDto>>(), grantedPermissionNames, base.FeatureChecker, base.AbpSession.TenantId.HasValue, base.PermissionManager);
			getUserPermissionsForEditOutput.GrantedPermissionNames = grantedPermissionNames;
			return getUserPermissionsForEditOutput;
		}

		[AbpAuthorize(new string[] { "Pages.Administration.Users.ChangePermissions" })]
		public async Task ResetUserSpecificPermissions(EntityDto<long> input)
		{
			User user = await GetMyUserAsync(input.Id);
			UserAccessPolicy access = _access;
			await access.CheckCanChangePermissionsAsync(await GrantedNamesAsync(user), await RolePermissionNamesAsync(user));
			await base.UserManager.ResetAllPermissionsAsync(user);
		}

		[AbpAuthorize(new string[] { "Pages.Administration.Users.ChangePermissions" })]
		public async Task UpdateUserPermissions(UpdateUserPermissionsInput input)
		{
			User user = await GetMyUserAsync(input.Id);
			UserAccessPolicy access = _access;
			await access.CheckCanChangePermissionsAsync(await GrantedNamesAsync(user), input.GrantedPermissionNames);
			IEnumerable<Permission> grantedPermissions = base.PermissionManager.GetPermissionsFromNamesByValidating(input.GrantedPermissionNames);
			await base.UserManager.SetGrantedPermissionsAsync(user, grantedPermissions);
		}

		public async Task CreateOrUpdateUser(CreateOrUpdateUserInput input)
		{
			await CheckPermissionAsync(input.User.Id.HasValue ? "Pages.Administration.Users.Edit" : "Pages.Administration.Users.Create");
			input.AssignedRoleNames = input.AssignedRoleNames ?? new string[0];
			if (input.User.Id.HasValue)
			{
				await UpdateUserAsync(input);
			}
			else
			{
				await CreateUserAsync(input);
			}
		}

		[AbpAuthorize(new string[] { "Pages.Administration.Users.Delete" })]
		public async Task DeleteUser(EntityDto<long> input)
		{
			if (input.Id == base.AbpSession.GetUserId())
			{
				throw new UserFriendlyException(L("YouCanNotDeleteOwnAccount"));
			}
			User user = await GetMyUserAsync(input.Id);
			CheckErrors(await base.UserManager.DeleteAsync(user));
		}

		public async Task UnlockUser(EntityDto<long> input)
		{
			await CheckPermissionAsync("Pages.Administration.Users.Edit");
			(await GetMyUserAsync(input.Id)).Unlock();
		}

		[AbpAuthorize(new string[] { "Pages.Administration.Users.Edit" })]
		protected virtual async Task UpdateUserAsync(CreateOrUpdateUserInput input)
		{
			User user = await GetMyUserAsync(input.User.Id.Value);
			IList<string> currentRoles = await base.UserManager.GetRolesAsync(user.Id);
			HashSet<string> hashSet = new HashSet<string>(currentRoles);
			hashSet.SymmetricExceptWith(input.AssignedRoleNames);
			if (hashSet.Any())
			{
				await _access.CheckNotOwnAccessAsync(user.Id);
			}
			await _access.CheckCanChangeRolesAsync(user.Id, currentRoles, input.AssignedRoleNames);
			input.User.MapTo(user);
			if (input.SetRandomPassword)
			{
				input.User.Password = User.CreateRandomPassword();
			}
			if (!input.User.Password.IsNullOrEmpty())
			{
				CheckErrors(await base.UserManager.ChangePasswordAsync(user, input.User.Password));
			}
			CheckErrors(await base.UserManager.UpdateAsync(user));
			await SetMyRolesAsync(user, input.AssignedRoleNames);
			if (input.SendActivationEmail)
			{
				user.SetNewEmailConfirmationCode();
				await _userEmailer.SendEmailActivationLinkAsync(user, input.User.Password);
			}
		}

		[AbpAuthorize(new string[] { "Pages.Administration.Users.Create" })]
		protected virtual async Task CreateUserAsync(CreateOrUpdateUserInput input)
		{
			if (base.AbpSession.TenantId.HasValue)
			{
				await _userPolicy.CheckMaxUserCountAsync(base.AbpSession.GetTenantId());
			}
			User user = input.User.MapTo<User>();
			user.TenantId = base.AbpSession.TenantId;
			if (!input.User.Password.IsNullOrEmpty())
			{
				CheckErrors(await base.UserManager.PasswordValidator.ValidateAsync(input.User.Password));
			}
			else
			{
				input.User.Password = User.CreateRandomPassword();
			}
			user.Password = new PasswordHasher().HashPassword(input.User.Password);
			user.ShouldChangePasswordOnNextLogin = input.User.ShouldChangePasswordOnNextLogin;
			await _access.CheckCanChangeRolesAsync(null, new string[0], input.AssignedRoleNames);
			user.Roles = new Collection<UserRole>();
			string[] assignedRoleNames = input.AssignedRoleNames;
			foreach (string roleName in assignedRoleNames)
			{
				Role role = await GetMyRoleByNameAsync(roleName);
				user.Roles.Add(new UserRole(base.AbpSession.TenantId, user.Id, role.Id));
			}
			CheckErrors(await base.UserManager.CreateAsync(user));
			await base.CurrentUnitOfWork.SaveChangesAsync();
			await _notificationSubscriptionManager.SubscribeToAllAvailableNotificationsAsync(user.ToUserIdentifier());
			await _appNotifier.WelcomeToTheApplicationAsync(user);
			if (input.SendActivationEmail)
			{
				user.SetNewEmailConfirmationCode();
				await _userEmailer.SendEmailActivationLinkAsync(user, input.User.Password);
			}
		}

		private IQueryable<Role> MyRoles()
		{
			int? tenantId = base.AbpSession.TenantId;
			return _roleManager.Roles.Where((Role r) => r.TenantId == tenantId);
		}

		private async Task<User> GetMyUserAsync(long id)
		{
			User user = await base.UserManager.GetUserByIdAsync(id);
			if (user.TenantId != base.AbpSession.TenantId)
			{
				throw new EntityNotFoundException(typeof(User), id);
			}
			await _access.CheckCanAccessUserAsync(user.Id);
			return user;
		}

		private async Task<List<long>> HiddenUserIdsAsync()
		{
			return (!(await _access.CurrentUserIsAdminAsync())) ? (await _access.GetAdminUserIdsAsync()) : new List<long>();
		}

		private async Task<List<string>> GrantedNamesAsync(User user)
		{
			return (await base.UserManager.GetGrantedPermissionsAsync(user)).Select((Permission p) => p.Name).ToList();
		}

		private async Task<List<string>> RolePermissionNamesAsync(User user)
		{
			HashSet<string> names = new HashSet<string>();
			foreach (string roleName in await base.UserManager.GetRolesAsync(user.Id))
			{
				Role role = await _roleManager.FindByNameAsync(roleName);
				if (role != null)
				{
					HashSet<string> hashSet = names;
					hashSet.UnionWith((await _roleManager.GetGrantedPermissionsAsync(role)).Select((Permission p) => p.Name));
				}
			}
			return names.ToList();
		}

		private async Task CheckPermissionAsync(string permissionName)
		{
			if (!(await base.PermissionChecker.IsGrantedAsync(permissionName)))
			{
				throw new AbpAuthorizationException(L("UserAccess_PermissionMissing", _access.Describe(permissionName)));
			}
		}

		private async Task<Role> GetMyRoleByNameAsync(string roleName)
		{
			Role role = await MyRoles().FirstOrDefaultAsync((Role r) => r.Name == roleName);
			if (role == null)
			{
				throw new UserFriendlyException(L("RoleNotFound", roleName));
			}
			return role;
		}

		private async Task SetMyRolesAsync(User user, IEnumerable<string> roleNames)
		{
			List<Role> wanted = new List<Role>();
			foreach (string name in (roleNames ?? Enumerable.Empty<string>()).Distinct())
			{
				List<Role> list = wanted;
				list.Add(await GetMyRoleByNameAsync(name));
			}
			long userId = user.Id;
			List<UserRole> current = await _userRoleRepository.GetAllListAsync((UserRole ur) => ur.UserId == userId);
			foreach (UserRole userRole in current.Where((UserRole ur) => wanted.All((Role r) => r.Id != ur.RoleId)))
			{
				await _userRoleRepository.DeleteAsync(userRole);
			}
			foreach (Role role in wanted.Where((Role r) => current.All((UserRole ur) => ur.RoleId != r.Id)))
			{
				await _userRoleRepository.InsertAsync(new UserRole(base.AbpSession.TenantId, user.Id, role.Id));
			}
		}

		private async Task FillRoleNames(List<UserListDto> userListDtos)
		{
			IEnumerable<int> distinctRoleIds = (from userListDto2 in userListDtos
				from userListRoleDto2 in userListDto2.Roles
				select userListRoleDto2.RoleId).Distinct();
			Dictionary<int, string> roleNames = new Dictionary<int, string>();
			foreach (int roleId in distinctRoleIds)
			{
				Dictionary<int, string> dictionary = roleNames;
				int key = roleId;
				dictionary[key] = (await _roleManager.GetRoleByIdAsync(roleId)).DisplayName;
			}
			foreach (UserListDto userListDto in userListDtos)
			{
				foreach (UserListDto.UserListRoleDto userListRoleDto in userListDto.Roles)
				{
					userListRoleDto.RoleName = roleNames[userListRoleDto.RoleId];
				}
				userListDto.Roles = userListDto.Roles.OrderBy((UserListDto.UserListRoleDto r) => r.RoleName).ToList();
			}
		}
	}
}
