using System;
using System.Collections.Generic;
using Abp.AutoMapper;
using StockPilot.MultiTenancy;

namespace StockPilot.Web.Models.Account
{
	public class TenantSelectionViewModel
	{
		[AutoMapFrom(new Type[] { typeof(Tenant) })]
		public class TenantInfo
		{
			public int Id { get; set; }

			public string TenancyName { get; set; }

			public string Name { get; set; }
		}

		public string Action { get; set; }

		public List<TenantInfo> Tenants { get; set; }
	}
}
