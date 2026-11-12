using System;
using Abp.Application.Services.Dto;
using Abp.AutoMapper;
using StockPilot.Authorization.Users;

namespace StockPilot.Sessions.Dto
{
	[AutoMapFrom(new Type[] { typeof(User) })]
	public class UserLoginInfoDto : EntityDto<long>
	{
		public string Name { get; set; }

		public string Surname { get; set; }

		public string UserName { get; set; }

		public string EmailAddress { get; set; }

		public string ProfilePictureId { get; set; }
	}
}
