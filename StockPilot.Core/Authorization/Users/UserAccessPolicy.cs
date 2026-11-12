using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Authorization.Users;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Localization;
using Abp.Runtime.Session;
using StockPilot.Authorization.Roles;

namespace StockPilot.Authorization.Users
{
	public class UserAccessPolicy : ITransientDependency
	{
		public const string AdminRoleName = "Admin";

		private readonly UserManager _userManager;

		private readonly RoleManager _roleManager;

		private readonly IRepository<UserRole, long> _userRoleRepository;

		private readonly IPermissionManager _permissionManager;

		private readonly IAbpSession _session;

		private readonly ILocalizationManager _localizationManager;

		public UserAccessPolicy(UserManager userManager, RoleManager roleManager, IRepository<UserRole, long> userRoleRepository, IPermissionManager permissionManager, IAbpSession session, ILocalizationManager localizationManager)
		{
			_userManager = userManager;
			_roleManager = roleManager;
			_userRoleRepository = userRoleRepository;
			_permissionManager = permissionManager;
			_session = session;
			_localizationManager = localizationManager;
		}

		public Task<Role> GetAdminRoleAsync()
		{
			int? tenantId = _session.TenantId;
			return Task.FromResult(_roleManager.Roles.FirstOrDefault((Role r) => r.TenantId == tenantId && r.Name == "Admin" && r.IsStatic));
		}

		public async Task<bool> IsAdminAsync(long userId)
		{
			Role admin = await GetAdminRoleAsync();
			bool flag = admin != null;
			if (flag)
			{
				flag = await _userRoleRepository.CountAsync((UserRole ur) => ur.UserId == userId && ur.RoleId == admin.Id) > 0;
			}
			return flag;
		}

		public Task<bool> CurrentUserIsAdminAsync()
		{
			if (!_session.UserId.HasValue)
			{
				return Task.FromResult(result: false);
			}
			return IsAdminAsync(_session.UserId.Value);
		}

		public async Task<List<long>> GetAdminUserIdsAsync()
		{
			Role admin = await GetAdminRoleAsync();
			if (admin == null)
			{
				return new List<long>();
			}
			return (await _userRoleRepository.GetAllListAsync((UserRole ur) => ur.RoleId == admin.Id)).Select((UserRole ur) => ur.UserId).Distinct().ToList();
		}

		public async Task CheckCanAccessUserAsync(long userId)
		{
			bool flag = !(await CurrentUserIsAdminAsync());
			if (flag)
			{
				flag = await IsAdminAsync(userId);
			}
			if (flag)
			{
				throw new AbpAuthorizationException(L("UserAccess_AdminProtected"));
			}
		}

		public async Task CheckNotOwnAccessAsync(long userId)
		{
			bool flag = _session.UserId == userId;
			if (flag)
			{
				flag = !(await CurrentUserIsAdminAsync());
			}
			if (flag)
			{
				throw new AbpAuthorizationException(L("UserAccess_OwnAccess"));
			}
		}

		public async Task<HashSet<string>> GetGrantablePermissionNamesAsync()
		{
			if (await CurrentUserIsAdminAsync() || !_session.UserId.HasValue)
			{
				return null;
			}
			User me = await _userManager.GetUserByIdAsync(_session.UserId.Value);
			return new HashSet<string>((await _userManager.GetGrantedPermissionsAsync(me)).Select((Permission p) => p.Name));
		}

		public async Task CheckCanChangePermissionsAsync(IEnumerable<string> before, IEnumerable<string> after)
		{
			HashSet<string> mine = await GetGrantablePermissionNamesAsync();
			if (mine != null)
			{
				HashSet<string> hashSet = new HashSet<string>(before ?? Enumerable.Empty<string>());
				HashSet<string> b = new HashSet<string>(after ?? Enumerable.Empty<string>());
				hashSet.SymmetricExceptWith(b);
				List<string> notMine = hashSet.Where((string name) => !mine.Contains(name)).ToList();
				if (notMine.Any())
				{
					throw new AbpAuthorizationException(string.Format(L("UserAccess_NotYourPermissions"), string.Join(", ", notMine.Select(Describe))));
				}
			}
		}

		public async Task CheckCanChangeRolesAsync(long? userId, IEnumerable<string> before, IEnumerable<string> after)
		{
			HashSet<string> a = new HashSet<string>(before ?? Enumerable.Empty<string>());
			HashSet<string> b = new HashSet<string>(after ?? Enumerable.Empty<string>());
			HashSet<string> changed = new HashSet<string>(a);
			changed.SymmetricExceptWith(b);
			if (!changed.Any())
			{
				return;
			}
			if (userId.HasValue && userId == _session.UserId && a.Contains("Admin") && !b.Contains("Admin"))
			{
				throw new AbpAuthorizationException(L("UserAccess_OwnAdminRole"));
			}
			HashSet<string> mine = await GetGrantablePermissionNamesAsync();
			if (mine == null)
			{
				return;
			}
			if (changed.Contains("Admin"))
			{
				throw new AbpAuthorizationException(L("UserAccess_AdminRole"));
			}
			foreach (string roleName in changed)
			{
				Role role = _roleManager.Roles.FirstOrDefault((Role r) => r.TenantId == _session.TenantId && r.Name == roleName);
				if (!(role == null) && (await _roleManager.GetGrantedPermissionsAsync(role)).Select((Permission p) => p.Name).Any((string name) => !mine.Contains(name)))
				{
					throw new AbpAuthorizationException(string.Format(L("UserAccess_RoleBeyondYours"), role.DisplayName));
				}
			}
		}

		public async Task<bool> CanChangeRoleAsync(Role role, HashSet<string> grantable)
		{
			if (grantable == null)
			{
				return true;
			}
			if (role.Name == "Admin" && role.IsStatic)
			{
				return false;
			}
			return (await _roleManager.GetGrantedPermissionsAsync(role)).All((Permission p) => grantable.Contains(p.Name));
		}

		public string Describe(string permissionName)
		{
			Permission permission = _permissionManager.GetPermissionOrNull(permissionName);
			if (permission == null)
			{
				return permissionName;
			}
			LocalizationContext context = new LocalizationContext(_localizationManager);
			string name = permission.DisplayName?.Localize(context) ?? permissionName;
			if (permission.Parent == null || permission.Children.Count != 0 || permission.Parent.Parent == null)
			{
				return name;
			}
			return (permission.Parent.DisplayName?.Localize(context) ?? permission.Parent.Name) + ": " + name;
		}

		private string L(string name)
		{
			return _localizationManager.GetString("StockPilot", name);
		}
	}
}
