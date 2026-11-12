using System.ComponentModel.DataAnnotations;
using Abp.Auditing;

namespace StockPilot.Web.Models.TenantRegistration
{
	public class TenantRegistrationViewModel
	{
		[Required]
		[StringLength(64)]
		[RegularExpression("^[a-zA-Z][a-zA-Z0-9_-]{1,}$")]
		public string TenancyName { get; set; }

		[Required]
		[StringLength(32)]
		public string Name { get; set; }

		[Required]
		[EmailAddress]
		[StringLength(256)]
		public string AdminEmailAddress { get; set; }

		[Required]
		[StringLength(32)]
		public string AdminName { get; set; }

		[Required]
		[StringLength(32)]
		public string AdminSurname { get; set; }

		[Required]
		[Phone]
		[StringLength(24)]
		public string AdminPhoneNumber { get; set; }

		[Required]
		[MinLength(6)]
		[StringLength(32)]
		[DisableAuditing]
		public string AdminPassword { get; set; }

		[StringLength(128)]
		public string ContactName { get; set; }

		[EmailAddress]
		[StringLength(256)]
		public string ContactEmail { get; set; }

		[Phone]
		[StringLength(32)]
		public string ContactPhone { get; set; }

		[StringLength(512)]
		public string Address { get; set; }

		[StringLength(128)]
		public string City { get; set; }

		[StringLength(128)]
		public string CountryName { get; set; }

		[StringLength(64)]
		public string BusinessType { get; set; }

		[StringLength(2000)]
		public string Notes { get; set; }

		public string ContactPhoneOrAdmin
		{
			get
			{
				if (!string.IsNullOrWhiteSpace(ContactPhone))
				{
					return ContactPhone.Trim();
				}
				return AdminPhoneNumber;
			}
		}

		public static string Clean(string value)
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				return value.Trim();
			}
			return null;
		}
	}
}
