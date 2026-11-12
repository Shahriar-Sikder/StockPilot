using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Abp.Application.Features;
using Abp.Authorization;
using Abp.Authorization.Roles;
using Abp.Authorization.Users;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using Abp.Localization;
using Abp.MultiTenancy;
using EntityFramework.DynamicFilters;
using Microsoft.AspNet.Identity;
using StockPilot.Authorization;
using StockPilot.Authorization.Roles;
using StockPilot.Authorization.Users;
using StockPilot.EntityFramework;

namespace StockPilot.Migrations.SeedData
{
	public class HostRoleAndUserCreator
	{
		private class RecordingPermissionContext : IPermissionDefinitionContext
		{
			private readonly List<Permission> _roots = new List<Permission>();

			public Permission CreatePermission(string name, ILocalizableString displayName = null, ILocalizableString description = null, MultiTenancySides multiTenancySides = MultiTenancySides.Tenant | MultiTenancySides.Host, IFeatureDependency featureDependency = null)
			{
				Permission permission = new Permission(name, displayName, description, multiTenancySides, featureDependency);
				_roots.Add(permission);
				return permission;
			}

			public Permission GetPermissionOrNull(string name)
			{
				return All().FirstOrDefault((Permission p) => p.Name == name);
			}

			public IEnumerable<Permission> All()
			{
				Stack<Permission> stack = new Stack<Permission>(_roots);
				while (stack.Count > 0)
				{
					Permission permission = stack.Pop();
					yield return permission;
					foreach (Permission child in permission.Children)
					{
						stack.Push(child);
					}
				}
			}
		}

		public const string InitialPasswordSetting = "App.HostAdminInitialPassword";

		public const string EmailSetting = "App.HostAdminEmailAddress";

		public const string DefaultEmailAddress = "admin@stockpilot.local";

		private readonly StockPilotDbContext _context;

		public HostRoleAndUserCreator(StockPilotDbContext context)
		{
			_context = context;
		}

		public void Create()
		{
			_context.DisableAllFilters();
			Role role = CreateAdminRole();
			GrantHostPermissions(role);
			CreateAdminUser(role);
		}

		private Role CreateAdminRole()
		{
			Role role = (from r in _context.Roles
				where r.TenantId == (int?)null && r.Name == "Admin" && !r.IsDeleted
				orderby r.Id
				select r).FirstOrDefault();
			if (role == null)
			{
				role = _context.Roles.Add(new Role(null, "Admin", "Admin")
				{
					IsStatic = true
				});
				_context.SaveChanges();
			}
			return role;
		}

		private void GrantHostPermissions(Role role)
		{
			HashSet<string> existing = new HashSet<string>((from p in _context.Permissions.OfType<RolePermissionSetting>()
				where p.TenantId == (int?)null && p.RoleId == role.Id
				select p.Name).ToList());
			foreach (string name in from item in HostPermissionNames()
				where !existing.Contains(item)
				select item)
			{
				_context.Permissions.Add(new RolePermissionSetting
				{
					TenantId = null,
					RoleId = role.Id,
					Name = name,
					IsGranted = true
				});
			}
			_context.SaveChanges();
		}

		private void CreateAdminUser(Role role)
		{
			if (!_context.Users.Any((User u) => u.TenantId == (int?)null && u.UserName == "admin" && !u.IsDeleted))
			{
				string password = ConfigurationManager.AppSettings["App.HostAdminInitialPassword"];
				if (!string.IsNullOrWhiteSpace(password))
				{
					string email = ConfigurationManager.AppSettings["App.HostAdminEmailAddress"];
					User user = _context.Users.Add(new User
					{
						TenantId = null,
						UserName = "admin",
						Name = "Host",
						Surname = "Admin",
						EmailAddress = (string.IsNullOrWhiteSpace(email) ? "admin@stockpilot.local" : email.Trim()),
						IsEmailConfirmed = true,
						IsActive = true,
						Password = new PasswordHasher().HashPassword(password)
					});
					_context.SaveChanges();
					_context.UserRoles.Add(new UserRole(null, user.Id, role.Id));
					_context.SaveChanges();
				}
			}
		}

		public static List<string> HostPermissionNames()
		{
			RecordingPermissionContext context = new RecordingPermissionContext();
			new AppAuthorizationProvider(isMultiTenancyEnabled: true).SetPermissions(context);
			return (from p in context.All()
				where p.MultiTenancySides.HasFlag(MultiTenancySides.Host)
				select p.Name).ToList();
		}
	}
}
