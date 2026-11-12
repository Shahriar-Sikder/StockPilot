using System;
using Abp.Extensions;
using Abp.Runtime.Validation;
using StockPilot.Dto;

namespace StockPilot.Auditing.Dto
{
	public class GetAuditLogsInput : PagedAndSortedInputDto, IShouldNormalize
	{
		public DateTime StartDate { get; set; }

		public DateTime EndDate { get; set; }

		public string UserName { get; set; }

		public string ServiceName { get; set; }

		public string MethodName { get; set; }

		public string BrowserInfo { get; set; }

		public bool? HasException { get; set; }

		public int? MinExecutionDuration { get; set; }

		public int? MaxExecutionDuration { get; set; }

		public void Normalize()
		{
			if (base.Sorting.IsNullOrWhiteSpace())
			{
				base.Sorting = "ExecutionTime DESC";
			}
			if (base.Sorting.IndexOf("UserName", StringComparison.InvariantCultureIgnoreCase) >= 0)
			{
				base.Sorting = "User." + base.Sorting;
			}
			else
			{
				base.Sorting = "AuditLog." + base.Sorting;
			}
		}
	}
}
