using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Dynamic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Auditing;
using Abp.Authorization;
using Abp.AutoMapper;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using StockPilot.Auditing.Dto;
using StockPilot.Auditing.Exporting;
using StockPilot.Authorization.Users;
using StockPilot.Dto;

namespace StockPilot.Auditing
{
	[DisableAuditing]
	[AbpAuthorize(new string[] { "Pages.Administration.AuditLogs" })]
	public class AuditLogAppService : StockPilotAppServiceBase, IAuditLogAppService, IApplicationService, ITransientDependency
	{
		private readonly IRepository<AuditLog, long> _auditLogRepository;

		private readonly IRepository<User, long> _userRepository;

		private readonly IAuditLogListExcelExporter _auditLogListExcelExporter;

		private readonly INamespaceStripper _namespaceStripper;

		private readonly UserAccessPolicy _access;

		public AuditLogAppService(IRepository<AuditLog, long> auditLogRepository, IRepository<User, long> userRepository, IAuditLogListExcelExporter auditLogListExcelExporter, INamespaceStripper namespaceStripper, UserAccessPolicy access)
		{
			_access = access;
			_auditLogRepository = auditLogRepository;
			_userRepository = userRepository;
			_auditLogListExcelExporter = auditLogListExcelExporter;
			_namespaceStripper = namespaceStripper;
		}

		public async Task<PagedResultDto<AuditLogListDto>> GetAuditLogs(GetAuditLogsInput input)
		{
			IQueryable<AuditLogAndUser> query = CreateAuditLogAndUsersQuery(input, await HiddenUserIdsAsync());
			int resultCount = await query.CountAsync();
			List<AuditLogListDto> auditLogListDtos = ConvertToAuditLogListDtos(await query.AsNoTracking().OrderBy(input.Sorting).PageBy(input)
				.ToListAsync());
			return new PagedResultDto<AuditLogListDto>(resultCount, auditLogListDtos);
		}

		public async Task<FileDto> GetAuditLogsToExcel(GetAuditLogsInput input)
		{
			List<AuditLogListDto> auditLogListDtos = ConvertToAuditLogListDtos(await (from al in CreateAuditLogAndUsersQuery(input, await HiddenUserIdsAsync()).AsNoTracking()
				orderby al.AuditLog.ExecutionTime descending
				select al).ToListAsync());
			return _auditLogListExcelExporter.ExportToFile(auditLogListDtos);
		}

		private List<AuditLogListDto> ConvertToAuditLogListDtos(List<AuditLogAndUser> results)
		{
			return results.Select(delegate(AuditLogAndUser result)
			{
				AuditLogListDto auditLogListDto = result.AuditLog.MapTo<AuditLogListDto>();
				auditLogListDto.UserName = ((result.User == null) ? null : result.User.UserName);
				auditLogListDto.ServiceName = _namespaceStripper.StripNameSpace(auditLogListDto.ServiceName);
				return auditLogListDto;
			}).ToList();
		}

		private async Task<List<long>> HiddenUserIdsAsync()
		{
			return (!(await _access.CurrentUserIsAdminAsync())) ? (await _access.GetAdminUserIdsAsync()) : new List<long>();
		}

		private IQueryable<AuditLogAndUser> CreateAuditLogAndUsersQuery(GetAuditLogsInput input, List<long> hiddenUserIds)
		{
			return (from auditLog in _auditLogRepository.GetAll()
				join user in _userRepository.GetAll() on auditLog.UserId equals user.Id into userJoin
				from joinedUser in userJoin.DefaultIfEmpty()
				where auditLog.ExecutionTime >= input.StartDate && auditLog.ExecutionTime <= input.EndDate
				select new AuditLogAndUser
				{
					AuditLog = auditLog,
					User = joinedUser
				}).WhereIf(hiddenUserIds.Count > 0, (AuditLogAndUser item) => item.AuditLog.UserId == (long?)null || !hiddenUserIds.Contains(item.AuditLog.UserId.Value)).WhereIf(!input.UserName.IsNullOrWhiteSpace(), (AuditLogAndUser item) => item.User.UserName.Contains(input.UserName)).WhereIf(!input.ServiceName.IsNullOrWhiteSpace(), (AuditLogAndUser item) => item.AuditLog.ServiceName.Contains(input.ServiceName))
				.WhereIf(!input.MethodName.IsNullOrWhiteSpace(), (AuditLogAndUser item) => item.AuditLog.MethodName.Contains(input.MethodName))
				.WhereIf(!input.BrowserInfo.IsNullOrWhiteSpace(), (AuditLogAndUser item) => item.AuditLog.BrowserInfo.Contains(input.BrowserInfo))
				.WhereIf(input.MinExecutionDuration.HasValue && input.MinExecutionDuration > 0, (AuditLogAndUser item) => item.AuditLog.ExecutionDuration >= input.MinExecutionDuration.Value)
				.WhereIf(input.MaxExecutionDuration.HasValue && input.MaxExecutionDuration < int.MaxValue, (AuditLogAndUser item) => item.AuditLog.ExecutionDuration <= input.MaxExecutionDuration.Value)
				.WhereIf(input.HasException == true, (AuditLogAndUser item) => item.AuditLog.Exception != null && item.AuditLog.Exception != "")
				.WhereIf(input.HasException == false, (AuditLogAndUser item) => item.AuditLog.Exception == null || item.AuditLog.Exception == "");
		}
	}
}
