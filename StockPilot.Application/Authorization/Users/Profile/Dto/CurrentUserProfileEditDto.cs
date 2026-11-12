using System;
using System.ComponentModel.DataAnnotations;
using Abp.AutoMapper;

namespace StockPilot.Authorization.Users.Profile.Dto
{
	[AutoMap(new Type[] { typeof(User) })]
	public class CurrentUserProfileEditDto
	{
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
		[StringLength(256)]
		public string EmailAddress { get; set; }

		[StringLength(24)]
		public string PhoneNumber { get; set; }

		public string Timezone { get; set; }
	}
}
