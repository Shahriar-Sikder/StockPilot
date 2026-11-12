using System;
using System.ComponentModel.DataAnnotations;
using Abp.AutoMapper;
using Abp.Localization;

namespace StockPilot.Localization.Dto
{
	[AutoMapFrom(new Type[] { typeof(ApplicationLanguage) })]
	public class ApplicationLanguageEditDto
	{
		public virtual int? Id { get; set; }

		[Required]
		[StringLength(10)]
		public virtual string Name { get; set; }

		[StringLength(128)]
		public virtual string Icon { get; set; }
	}
}
