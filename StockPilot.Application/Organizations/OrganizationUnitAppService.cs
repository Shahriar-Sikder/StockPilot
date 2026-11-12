using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Dynamic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Authorization.Users;
using Abp.AutoMapper;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.Organizations;
using StockPilot.Authorization.Users;
using StockPilot.Organizations.Dto;

namespace StockPilot.Organizations
{
	[AbpAuthorize(new string[] { "Pages.Administration.OrganizationUnits" })]
	public class OrganizationUnitAppService : StockPilotAppServiceBase, IOrganizationUnitAppService, IApplicationService, ITransientDependency
	{
		private readonly OrganizationUnitManager _organizationUnitManager;

		private readonly IRepository<OrganizationUnit, long> _organizationUnitRepository;

		private readonly IRepository<UserOrganizationUnit, long> _userOrganizationUnitRepository;

		private readonly UserAccessPolicy _access;

		public OrganizationUnitAppService(OrganizationUnitManager organizationUnitManager, IRepository<OrganizationUnit, long> organizationUnitRepository, IRepository<UserOrganizationUnit, long> userOrganizationUnitRepository, UserAccessPolicy access)
		{
			_access = access;
			_organizationUnitManager = organizationUnitManager;
			_organizationUnitRepository = organizationUnitRepository;
			_userOrganizationUnitRepository = userOrganizationUnitRepository;
		}

		public async Task<ListResultDto<OrganizationUnitDto>> GetOrganizationUnits()
		{
			return new ListResultDto<OrganizationUnitDto>((await (from ou in _organizationUnitRepository.GetAll()
				join uou in _userOrganizationUnitRepository.GetAll() on ou.Id equals uou.OrganizationUnitId into g
				select new
				{
					ou = ou,
					memberCount = g.Count()
				}).ToListAsync()).Select(item =>
			{
				OrganizationUnitDto organizationUnitDto = item.ou.MapTo<OrganizationUnitDto>();
				organizationUnitDto.MemberCount = item.memberCount;
				return organizationUnitDto;
			}).ToList());
		}

		public async Task<PagedResultDto<OrganizationUnitUserListDto>> GetOrganizationUnitUsers(GetOrganizationUnitUsersInput input)
		{
			List<long> list = ((!(await _access.CurrentUserIsAdminAsync())) ? (await _access.GetAdminUserIdsAsync()) : new List<long>());
			List<long> hidden = list;
			var query = from uou in _userOrganizationUnitRepository.GetAll()
				join ou in _organizationUnitRepository.GetAll() on uou.OrganizationUnitId equals ou.Id
				join user in base.UserManager.Users on uou.UserId equals user.Id
				where uou.OrganizationUnitId == input.Id && !hidden.Contains(user.Id)
				select new { uou, user };
			return new PagedResultDto<OrganizationUnitUserListDto>(await query.CountAsync(), (await query.OrderBy(input.Sorting).PageBy(input).ToListAsync()).Select(item =>
			{
				OrganizationUnitUserListDto organizationUnitUserListDto = item.user.MapTo<OrganizationUnitUserListDto>();
				organizationUnitUserListDto.AddedTime = item.uou.CreationTime;
				return organizationUnitUserListDto;
			}).ToList());
		}

		[AbpAuthorize(new string[] { "Pages.Administration.OrganizationUnits.ManageOrganizationTree" })]
		public async Task<OrganizationUnitDto> CreateOrganizationUnit(CreateOrganizationUnitInput input)
		{
			OrganizationUnit organizationUnit = new OrganizationUnit(base.AbpSession.TenantId, input.DisplayName, input.ParentId);
			await _organizationUnitManager.CreateAsync(organizationUnit);
			await base.CurrentUnitOfWork.SaveChangesAsync();
			return organizationUnit.MapTo<OrganizationUnitDto>();
		}

		[AbpAuthorize(new string[] { "Pages.Administration.OrganizationUnits.ManageOrganizationTree" })]
		public async Task<OrganizationUnitDto> UpdateOrganizationUnit(UpdateOrganizationUnitInput input)
		{
			OrganizationUnit organizationUnit = await _organizationUnitRepository.GetAsync(input.Id);
			organizationUnit.DisplayName = input.DisplayName;
			await _organizationUnitManager.UpdateAsync(organizationUnit);
			return await CreateOrganizationUnitDto(organizationUnit);
		}

		[AbpAuthorize(new string[] { "Pages.Administration.OrganizationUnits.ManageOrganizationTree" })]
		public async Task<OrganizationUnitDto> MoveOrganizationUnit(MoveOrganizationUnitInput input)
		{
			await _organizationUnitManager.MoveAsync(input.Id, input.NewParentId);
			return await CreateOrganizationUnitDto(await _organizationUnitRepository.GetAsync(input.Id));
		}

		[AbpAuthorize(new string[] { "Pages.Administration.OrganizationUnits.ManageOrganizationTree" })]
		public async Task DeleteOrganizationUnit(EntityDto<long> input)
		{
			await _organizationUnitManager.DeleteAsync(input.Id);
		}

		[AbpAuthorize(new string[] { "Pages.Administration.OrganizationUnits.ManageMembers" })]
		public async Task AddUserToOrganizationUnit(UserToOrganizationUnitInput input)
		{
			await _access.CheckCanAccessUserAsync(input.UserId);
			await base.UserManager.AddToOrganizationUnitAsync(input.UserId, input.OrganizationUnitId);
		}

		[AbpAuthorize(new string[] { "Pages.Administration.OrganizationUnits.ManageMembers" })]
		public async Task RemoveUserFromOrganizationUnit(UserToOrganizationUnitInput input)
		{
			await _access.CheckCanAccessUserAsync(input.UserId);
			await base.UserManager.RemoveFromOrganizationUnitAsync(input.UserId, input.OrganizationUnitId);
		}

		[AbpAuthorize(new string[] { "Pages.Administration.OrganizationUnits.ManageMembers" })]
		public async Task<bool> IsInOrganizationUnit(UserToOrganizationUnitInput input)
		{
			return await base.UserManager.IsInOrganizationUnitAsync(input.UserId, input.OrganizationUnitId);
		}

		private async Task<OrganizationUnitDto> CreateOrganizationUnitDto(OrganizationUnit organizationUnit)
		{
			OrganizationUnitDto dto = organizationUnit.MapTo<OrganizationUnitDto>();
			OrganizationUnitDto organizationUnitDto = dto;
			organizationUnitDto.MemberCount = await _userOrganizationUnitRepository.CountAsync((UserOrganizationUnit uou) => uou.OrganizationUnitId == organizationUnit.Id);
			return dto;
		}
	}
}
