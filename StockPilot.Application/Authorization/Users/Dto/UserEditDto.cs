using System.ComponentModel.DataAnnotations;
using Abp.Auditing;
using Abp.Domain.Entities;

namespace StockPilot.Authorization.Users.Dto
{
	public class UserEditDto : IPassivable
	{
		public long? Id { get; set; }

		[Required]
		[StringLength(32)]
		public string Name { get; set; }

		[Required]
		[StringLength(32)]
		public string Surname { get; set; }

		[Required]
		[StringLength(32)]
		public string UserName { get; set; }

		[Required]
		[EmailAddress]
		[StringLength(256)]
		public string EmailAddress { get; set; }

		[StringLength(24)]
		public string PhoneNumber { get; set; }

		[StringLength(32)]
		[DisableAuditing]
		public string Password { get; set; }

		[DisableAuditing]
		public string NewPassword { get; set; }

		public bool IsActive { get; set; }

		public bool ShouldChangePasswordOnNextLogin { get; set; }

		public virtual bool IsTwoFactorEnabled { get; set; }

		public virtual bool IsLockoutEnabled { get; set; }
	}
}
