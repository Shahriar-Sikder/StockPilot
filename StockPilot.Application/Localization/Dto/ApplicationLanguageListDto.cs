using System;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using Abp.Localization;

namespace StockPilot.Localization.Dto
{
	[AutoMapFrom(new Type[] { typeof(ApplicationLanguage) })]
	public class ApplicationLanguageListDto : FullAuditedEntityDto
	{
		public virtual int? TenantId { get; set; }

		public virtual string Name { get; set; }

		public virtual string DisplayName { get; set; }

		public virtual string Icon { get; set; }
	}
}
