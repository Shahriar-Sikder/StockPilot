using System;
using System.Linq;
using Abp.Authorization.Users;
using Abp.Extensions;
using Microsoft.AspNet.Identity;

namespace StockPilot.Authorization.Users
{
	public class User : AbpUser<User>
	{
		public const int MinPlainPasswordLength = 6;

		public const int MaxPhoneNumberLength = 24;

		public virtual Guid? ProfilePictureId { get; set; }

		public virtual bool ShouldChangePasswordOnNextLogin { get; set; }

		public User()
		{
			IsLockoutEnabled = true;
			IsTwoFactorEnabled = true;
		}

		public static User CreateTenantAdminUser(int tenantId, string emailAddress, string password, string phoneNumber = null, string name = null, string surname = null)
		{
			return new User
			{
				TenantId = tenantId,
				UserName = "admin",
				Name = (string.IsNullOrWhiteSpace(name) ? "admin" : name.Trim()),
				Surname = (string.IsNullOrWhiteSpace(surname) ? "admin" : surname.Trim()),
				EmailAddress = emailAddress,
				PhoneNumber = NormalizePhoneNumber(phoneNumber),
				Password = new PasswordHasher().HashPassword(password)
			};
		}

		public static string NormalizePhoneNumber(string phoneNumber)
		{
			if (string.IsNullOrWhiteSpace(phoneNumber))
			{
				return null;
			}
			string text = phoneNumber.Trim();
			string digits = new string(text.Where(char.IsDigit).ToArray());
			if (!text.StartsWith("+"))
			{
				return digits;
			}
			return "+" + digits;
		}

		public static string CreateRandomPassword()
		{
			return Guid.NewGuid().ToString("N").Truncate(16);
		}

		public void Unlock()
		{
			AccessFailedCount = 0;
			LockoutEndDateUtc = null;
		}

		public void Active()
		{
			IsActive = true;
		}

		public void DeActive()
		{
			IsActive = false;
		}

		public void OTPContirmed()
		{
			IsPhoneNumberConfirmed = true;
		}
	}
}
