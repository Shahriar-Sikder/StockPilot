using System;
using System.ComponentModel.DataAnnotations;
using Abp.MultiTenancy;
using StockPilot.Authorization.Users;

namespace StockPilot.MultiTenancy
{
	public class Tenant : AbpTenant<User>
	{
		public const int MaxLogoMimeTypeLength = 64;

		public const int MaxContactNameLength = 128;

		public const int MaxContactEmailLength = 256;

		public const int MaxContactPhoneLength = 32;

		public const int MaxAddressLength = 512;

		public const int MaxCityLength = 128;

		public const int MaxCountryNameLength = 128;

		public const int MaxBusinessTypeLength = 64;

		public const int MaxNotesLength = 2000;

		[MaxLength(128)]
		public virtual string ContactName { get; set; }

		[MaxLength(256)]
		public virtual string ContactEmail { get; set; }

		[MaxLength(32)]
		public virtual string ContactPhone { get; set; }

		[MaxLength(512)]
		public virtual string Address { get; set; }

		[MaxLength(128)]
		public virtual string City { get; set; }

		[MaxLength(128)]
		public virtual string CountryName { get; set; }

		[MaxLength(64)]
		public virtual string BusinessType { get; set; }

		[MaxLength(2000)]
		public virtual string Notes { get; set; }

		public virtual Guid? CustomCssId { get; set; }

		public virtual Guid? LogoId { get; set; }

		[MaxLength(64)]
		public virtual string LogoFileType { get; set; }

		public virtual DateTime? SubscriptionEndDate { get; set; }

		protected Tenant()
		{
		}

		public Tenant(string tenancyName, string name)
			: base(tenancyName, name)
		{
		}

		public virtual bool HasLogo()
		{
			if (LogoId.HasValue)
			{
				return LogoFileType != null;
			}
			return false;
		}

		public void ClearLogo()
		{
			LogoId = null;
			LogoFileType = null;
		}
	}
}
