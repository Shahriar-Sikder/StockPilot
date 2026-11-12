using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Authorization.Roles;
using Abp.AutoMapper;
using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Extensions;
using Abp.Linq.Extensions;
using StockPilot.Authorization.Permissions;
using StockPilot.Authorization.Permissions.Dto;
using StockPilot.Authorization.Roles.Dto;
using StockPilot.Authorization.Users;

namespace StockPilot.Authorization.Roles
{
	[AbpAuthorize(new string[] { "Pages.Administration.Roles" })]
	public class RoleAppService : StockPilotAppServiceBase, IRoleAppService, IApplicationService, ITransientDependency
	{
		private readonly RoleManager _roleManager;

		private readonly UserAccessPolicy _access;

		public RoleAppService(RoleManager roleManager, UserAccessPolicy access)
		{
			_roleManager = roleManager;
			_access = access;
		}

		public async Task<ListResultDto<RoleListDto>> GetRoles(GetRolesInput input)
		{
			int? tenantId = base.AbpSession.TenantId;
			return new ListResultDto<RoleListDto>((await _roleManager.Roles.Where((Role r) => r.TenantId == tenantId).WhereIf(!input.Permission.IsNullOrWhiteSpace(), (Role r) => r.Permissions.Any((RolePermissionSetting rp) => rp.Name == input.Permission && rp.IsGranted)).ToListAsync()).MapTo<List<RoleListDto>>());
		}

		[AbpAuthorize(new string[] { "Pages.Administration.Roles.Create", "Pages.Administration.Roles.Edit" })]
		public async Task<GetRoleForEditOutput> GetRoleForEdit(NullableIdDto input)
		{
			IReadOnlyList<Permission> permissions = base.PermissionManager.GetAllPermissions();
			Permission[] grantedPermissions = new Permission[0];
			RoleEditDto roleEditDto;
			if (input.Id.HasValue)
			{
				Role role = await GetMyRoleAsync(input.Id.Value);
				await CheckNotAdminRoleAsync(role);
				grantedPermissions = (await _roleManager.GetGrantedPermissionsAsync(role)).ToArray();
				roleEditDto = role.MapTo<RoleEditDto>();
			}
			else
			{
				roleEditDto = new RoleEditDto();
			}
			List<string> grantedPermissionNames = grantedPermissions.Select((Permission p) => p.Name).ToList();
			HashSet<string> grantable = await _access.GetGrantablePermissionNamesAsync();
			GetRoleForEditOutput getRoleForEditOutput = new GetRoleForEditOutput
			{
				Role = roleEditDto,
				GrantablePermissionNames = grantable?.ToList()
			};
			GetRoleForEditOutput getRoleForEditOutput2 = getRoleForEditOutput;
			getRoleForEditOutput2.Permissions = await PermissionFeatureFilter.RemoveDisabledFeaturePermissions(permissions.MapTo<List<FlatPermissionDto>>(), grantedPermissionNames, base.FeatureChecker, base.AbpSession.TenantId.HasValue, base.PermissionManager);
			getRoleForEditOutput.GrantedPermissionNames = grantedPermissionNames;
			return getRoleForEditOutput;
		}

		public async Task CreateOrUpdateRole(CreateOrUpdateRoleInput input)
		{
			string permission = (input.Role.Id.HasValue ? "Pages.Administration.Roles.Edit" : "Pages.Administration.Roles.Create");
			if (!(await base.PermissionChecker.IsGrantedAsync(permission)))
			{
				throw new AbpAuthorizationException(L("UserAccess_PermissionMissing", _access.Describe(permission)));
			}
			if (input.Role.Id.HasValue)
			{
				await UpdateRoleAsync(input);
			}
			else
			{
				await CreateRoleAsync(input);
			}
		}

		[AbpAuthorize(new string[] { "Pages.Administration.Roles.Delete" })]
		public async Task DeleteRole(EntityDto input)
		{
			Role role = await GetMyRoleAsync(input.Id);
			await CheckNotAdminRoleAsync(role);
			UserAccessPolicy access = _access;
			await access.CheckCanChangePermissionsAsync((await _roleManager.GetGrantedPermissionsAsync(role)).Select((Permission p) => p.Name), new string[0]);
			CheckErrors(await _roleManager.DeleteAsync(role));
		}

		[AbpAuthorize(new string[] { "Pages.Administration.Roles.Edit" })]
		protected virtual async Task UpdateRoleAsync(CreateOrUpdateRoleInput input)
		{
			Role role = await GetMyRoleAsync(input.Role.Id.Value);
			await CheckNotAdminRoleAsync(role);
			UserAccessPolicy access = _access;
			await access.CheckCanChangePermissionsAsync((await _roleManager.GetGrantedPermissionsAsync(role)).Select((Permission p) => p.Name), input.GrantedPermissionNames);
			role.DisplayName = input.Role.DisplayName;
			role.IsDefault = input.Role.IsDefault;
			await UpdateGrantedPermissionsAsync(role, input.GrantedPermissionNames);
		}

		private async Task CheckNotAdminRoleAsync(Role role)
		{
			bool flag = role.IsStatic && role.Name == "Admin";
			if (flag)
			{
				flag = !(await _access.CurrentUserIsAdminAsync());
			}
			if (flag)
			{
				throw new AbpAuthorizationException(L("UserAccess_AdminRoleProtected"));
			}
		}

		private async Task<Role> GetMyRoleAsync(int id)
		{
			Role obj = await _roleManager.GetRoleByIdAsync(id);
			if (obj.TenantId != base.AbpSession.TenantId)
			{
				throw new EntityNotFoundException(typeof(Role), id);
			}
			return obj;
		}

		[AbpAuthorize(new string[] { "Pages.Administration.Roles.Create" })]
		protected virtual async Task CreateRoleAsync(CreateOrUpdateRoleInput input)
		{
			await _access.CheckCanChangePermissionsAsync(new string[0], input.GrantedPermissionNames);
			Role role = new Role(base.AbpSession.TenantId, input.Role.DisplayName)
			{
				IsDefault = input.Role.IsDefault
			};
			CheckErrors(await _roleManager.CreateAsync(role));
			await base.CurrentUnitOfWork.SaveChangesAsync();
			await UpdateGrantedPermissionsAsync(role, input.GrantedPermissionNames);
		}

		private async Task UpdateGrantedPermissionsAsync(Role role, List<string> grantedPermissionNames)
		{
			IEnumerable<Permission> grantedPermissions = base.PermissionManager.GetPermissionsFromNamesByValidating(grantedPermissionNames);
			await _roleManager.SetGrantedPermissionsAsync(role, grantedPermissions);
		}
	}
}
