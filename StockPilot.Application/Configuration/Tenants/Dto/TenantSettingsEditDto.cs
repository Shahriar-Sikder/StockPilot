using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Runtime.Validation;
using StockPilot.Configuration.Host.Dto;

namespace StockPilot.Configuration.Tenants.Dto
{
	public class TenantSettingsEditDto
	{
		public GeneralSettingsEditDto General { get; set; }

		public EcommSettingsEditDto Ecomm { get; set; }

		[Required]
		public TenantUserManagementSettingsEditDto UserManagement { get; set; }

		public EmailSettingsEditDto Email { get; set; }

		public LdapSettingsEditDto Ldap { get; set; }

		[Required]
		public SecuritySettingsEditDto Security { get; set; }

		public void ValidateHostSettings()
		{
			List<ValidationResult> validationErrors = new List<ValidationResult>();
			if (General == null)
			{
				validationErrors.Add(new ValidationResult("General settings can not be null", new string[1] { "General" }));
			}
			if (Email == null)
			{
				validationErrors.Add(new ValidationResult("Email settings can not be null", new string[1] { "Email" }));
			}
			if (validationErrors.Count > 0)
			{
				throw new AbpValidationException("Method arguments are not valid! See ValidationErrors for details.", validationErrors);
			}
		}
	}
}
