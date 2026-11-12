using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;

namespace StockPilot.Localization
{
	public class GetLanguageTextsInput : IPagedResultRequest, ILimitedResultRequest, ISortedResultRequest, IShouldNormalize
	{
		[Range(0, int.MaxValue)]
		public int MaxResultCount { get; set; }

		[Range(0, int.MaxValue)]
		public int SkipCount { get; set; }

		public string Sorting { get; set; }

		[Required]
		[MaxLength(128)]
		public string SourceName { get; set; }

		[StringLength(10)]
		public string BaseLanguageName { get; set; }

		[Required]
		[StringLength(10, MinimumLength = 2)]
		public string TargetLanguageName { get; set; }

		public string TargetValueFilter { get; set; }

		public string FilterText { get; set; }

		public void Normalize()
		{
			if (TargetValueFilter.IsNullOrEmpty())
			{
				TargetValueFilter = "ALL";
			}
		}
	}
}
