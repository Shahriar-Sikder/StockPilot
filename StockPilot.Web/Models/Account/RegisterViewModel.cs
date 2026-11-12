using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Auditing;
using Abp.Extensions;
using StockPilot.Security;
using StockPilot.Validation;

namespace StockPilot.Web.Models.Account
{
	public class RegisterViewModel : IValidatableObject
	{
		[StringLength(64)]
		public string TenancyName { get; set; }

		[Required]
		[StringLength(32)]
		public string Name { get; set; }

		[Required]
		[StringLength(32)]
		public string Surname { get; set; }

		[StringLength(32)]
		public string UserName { get; set; }

		[Required]
		[EmailAddress]
		[StringLength(256)]
		public string EmailAddress { get; set; }

		[StringLength(32)]
		[DisableAuditing]
		public string Password { get; set; }

		public bool IsExternalLogin { get; set; }

		public PasswordComplexitySetting PasswordComplexitySetting { get; set; }

		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			if (!UserName.IsNullOrEmpty() && !UserName.Equals(EmailAddress) && new ValidationHelper().IsEmail(UserName))
			{
				yield return new ValidationResult("Username cannot be an email address unless it's same with your email address !");
			}
		}
	}
}
