using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Builders;
using System.Data.Entity.Migrations.Infrastructure;
using System.Data.Entity.Migrations.Model;
using System.Resources;

namespace StockPilot.Migrations
{
	[GeneratedCode("EntityFramework.Migrations", "6.4.4")]
	public sealed class Add_Stock_Tables : DbMigration, IMigrationMetadata
	{
		private readonly ResourceManager Resources = new ResourceManager(typeof(Add_Stock_Tables));

		string IMigrationMetadata.Id => "202611031622227_Add_Stock_Tables";

		string IMigrationMetadata.Source => null;

		string IMigrationMetadata.Target => Resources.GetString("Target");

		public override void Up()
		{
			CreateTable("dbo.AbpAuditLogs", delegate(ColumnBuilder c)
			{
				ColumnModel id = c.Long(false, identity: true);
				ColumnModel tenantId = c.Int();
				ColumnModel userId = c.Long();
				int? maxLength = 256;
				ColumnModel serviceName = c.String(null, maxLength);
				maxLength = 256;
				ColumnModel methodName = c.String(null, maxLength);
				maxLength = 1024;
				ColumnModel parameters = c.String(null, maxLength);
				ColumnModel executionTime = c.DateTime(false);
				ColumnModel executionDuration = c.Int(false);
				maxLength = 64;
				ColumnModel clientIpAddress = c.String(null, maxLength);
				maxLength = 128;
				ColumnModel clientName = c.String(null, maxLength);
				maxLength = 256;
				ColumnModel browserInfo = c.String(null, maxLength);
				maxLength = 2000;
				ColumnModel exception = c.String(null, maxLength);
				ColumnModel impersonatorUserId = c.Long();
				ColumnModel impersonatorTenantId = c.Int();
				maxLength = 2000;
				return new
				{
					Id = id,
					TenantId = tenantId,
					UserId = userId,
					ServiceName = serviceName,
					MethodName = methodName,
					Parameters = parameters,
					ExecutionTime = executionTime,
					ExecutionDuration = executionDuration,
					ClientIpAddress = clientIpAddress,
					ClientName = clientName,
					BrowserInfo = browserInfo,
					Exception = exception,
					ImpersonatorUserId = impersonatorUserId,
					ImpersonatorTenantId = impersonatorTenantId,
					CustomData = c.String(null, maxLength)
				};
			}, new Dictionary<string, object> { { "DynamicFilter_AuditLog_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" } }).PrimaryKey(t => t.Id);
			CreateTable("dbo.AbpBackgroundJobs", (ColumnBuilder c) => new
			{
				Id = c.Long(false, identity: true),
				JobType = c.String(false, 512),
				JobArgs = c.String(false),
				TryCount = c.Short(false),
				NextTryTime = c.DateTime(false),
				LastTryTime = c.DateTime(),
				IsAbandoned = c.Boolean(false),
				Priority = c.Byte(false),
				CreationTime = c.DateTime(false),
				CreatorUserId = c.Long()
			}).PrimaryKey(t => t.Id).Index(t => new { t.IsAbandoned, t.NextTryTime });
			CreateTable("dbo.AppBinaryObjects", (ColumnBuilder c) => new
			{
				Id = c.Guid(false),
				TenantId = c.Int(),
				Bytes = c.Binary(false)
			}, new Dictionary<string, object> { { "DynamicFilter_BinaryObject_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" } }).PrimaryKey(t => t.Id);
			CreateTable("dbo.AbpFeatures", (ColumnBuilder c) => new
			{
				Id = c.Long(false, identity: true),
				Name = c.String(false, 128),
				Value = c.String(false, 2000),
				CreationTime = c.DateTime(false),
				CreatorUserId = c.Long(),
				EditionId = c.Int(),
				TenantId = c.Int(),
				Discriminator = c.String(false, 128)
			}, new Dictionary<string, object> { { "DynamicFilter_TenantFeatureSetting_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" } }).PrimaryKey(t => t.Id).ForeignKey("dbo.AbpEditions", t => t.EditionId, cascadeDelete: true).Index(t => t.EditionId);
			CreateTable("dbo.AbpEditions", (ColumnBuilder c) => new
			{
				Id = c.Int(false, identity: true),
				Name = c.String(false, 32),
				DisplayName = c.String(false, 64),
				IsDeleted = c.Boolean(false),
				DeleterUserId = c.Long(),
				DeletionTime = c.DateTime(),
				LastModificationTime = c.DateTime(),
				LastModifierUserId = c.Long(),
				CreationTime = c.DateTime(false),
				CreatorUserId = c.Long()
			}, new Dictionary<string, object> { { "DynamicFilter_Edition_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" } }).PrimaryKey(t => t.Id);
			CreateTable("dbo.AbpLanguages", delegate(ColumnBuilder c)
			{
				ColumnModel id = c.Int(false, identity: true);
				ColumnModel tenantId = c.Int();
				ColumnModel name = c.String(false, 10);
				ColumnModel displayName = c.String(false, 64);
				int? maxLength = 128;
				return new
				{
					Id = id,
					TenantId = tenantId,
					Name = name,
					DisplayName = displayName,
					Icon = c.String(null, maxLength),
					IsDisabled = c.Boolean(false),
					IsDeleted = c.Boolean(false),
					DeleterUserId = c.Long(),
					DeletionTime = c.DateTime(),
					LastModificationTime = c.DateTime(),
					LastModifierUserId = c.Long(),
					CreationTime = c.DateTime(false),
					CreatorUserId = c.Long()
				};
			}, new Dictionary<string, object>
			{
				{ "DynamicFilter_ApplicationLanguage_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_ApplicationLanguage_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			}).PrimaryKey(t => t.Id);
			CreateTable("dbo.AbpLanguageTexts", (ColumnBuilder c) => new
			{
				Id = c.Long(false, identity: true),
				TenantId = c.Int(),
				LanguageName = c.String(false, 10),
				Source = c.String(false, 128),
				Key = c.String(false, 256),
				Value = c.String(false),
				LastModificationTime = c.DateTime(),
				LastModifierUserId = c.Long(),
				CreationTime = c.DateTime(false),
				CreatorUserId = c.Long()
			}, new Dictionary<string, object> { { "DynamicFilter_ApplicationLanguageText_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" } }).PrimaryKey(t => t.Id);
			CreateTable("dbo.AbpNotifications", delegate(ColumnBuilder c)
			{
				ColumnModel id = c.Guid(false);
				ColumnModel notificationName = c.String(false, 96);
				ColumnModel data = c.String();
				int? maxLength = 512;
				ColumnModel dataTypeName = c.String(null, maxLength);
				maxLength = 250;
				ColumnModel entityTypeName = c.String(null, maxLength);
				maxLength = 512;
				ColumnModel entityTypeAssemblyQualifiedName = c.String(null, maxLength);
				maxLength = 96;
				return new
				{
					Id = id,
					NotificationName = notificationName,
					Data = data,
					DataTypeName = dataTypeName,
					EntityTypeName = entityTypeName,
					EntityTypeAssemblyQualifiedName = entityTypeAssemblyQualifiedName,
					EntityId = c.String(null, maxLength),
					Severity = c.Byte(false),
					UserIds = c.String(),
					ExcludedUserIds = c.String(),
					TenantIds = c.String(),
					CreationTime = c.DateTime(false),
					CreatorUserId = c.Long()
				};
			}).PrimaryKey(t => t.Id);
			CreateTable("dbo.AbpNotificationSubscriptions", delegate(ColumnBuilder c)
			{
				ColumnModel id = c.Guid(false);
				ColumnModel tenantId = c.Int();
				ColumnModel userId = c.Long(false);
				int? maxLength = 96;
				ColumnModel notificationName = c.String(null, maxLength);
				maxLength = 250;
				ColumnModel entityTypeName = c.String(null, maxLength);
				maxLength = 512;
				ColumnModel entityTypeAssemblyQualifiedName = c.String(null, maxLength);
				maxLength = 96;
				return new
				{
					Id = id,
					TenantId = tenantId,
					UserId = userId,
					NotificationName = notificationName,
					EntityTypeName = entityTypeName,
					EntityTypeAssemblyQualifiedName = entityTypeAssemblyQualifiedName,
					EntityId = c.String(null, maxLength),
					CreationTime = c.DateTime(false),
					CreatorUserId = c.Long()
				};
			}, new Dictionary<string, object> { { "DynamicFilter_NotificationSubscriptionInfo_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" } }).PrimaryKey(t => t.Id).Index(t => new { t.NotificationName, t.EntityTypeName, t.EntityId, t.UserId });
			CreateTable("dbo.AbpOrganizationUnits", (ColumnBuilder c) => new
			{
				Id = c.Long(false, identity: true),
				TenantId = c.Int(),
				ParentId = c.Long(),
				Code = c.String(false, 95),
				DisplayName = c.String(false, 128),
				IsDeleted = c.Boolean(false),
				DeleterUserId = c.Long(),
				DeletionTime = c.DateTime(),
				LastModificationTime = c.DateTime(),
				LastModifierUserId = c.Long(),
				CreationTime = c.DateTime(false),
				CreatorUserId = c.Long()
			}, new Dictionary<string, object>
			{
				{ "DynamicFilter_OrganizationUnit_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_OrganizationUnit_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			}).PrimaryKey(t => t.Id).ForeignKey("dbo.AbpOrganizationUnits", t => t.ParentId).Index(t => t.ParentId);
			CreateTable("dbo.AbpPermissions", (ColumnBuilder c) => new
			{
				Id = c.Long(false, identity: true),
				TenantId = c.Int(),
				Name = c.String(false, 128),
				IsGranted = c.Boolean(false),
				CreationTime = c.DateTime(false),
				CreatorUserId = c.Long(),
				RoleId = c.Int(),
				UserId = c.Long(),
				Discriminator = c.String(false, 128)
			}, new Dictionary<string, object>
			{
				{ "DynamicFilter_PermissionSetting_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_RolePermissionSetting_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_UserPermissionSetting_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			}).PrimaryKey(t => t.Id).ForeignKey("dbo.AbpUsers", t => t.UserId, cascadeDelete: true).ForeignKey("dbo.AbpRoles", t => t.RoleId, cascadeDelete: true)
				.Index(t => t.RoleId)
				.Index(t => t.UserId);
			CreateTable("dbo.AbpRoles", (ColumnBuilder c) => new
			{
				Id = c.Int(false, identity: true),
				TenantId = c.Int(),
				Name = c.String(false, 32),
				DisplayName = c.String(false, 64),
				IsStatic = c.Boolean(false),
				IsDefault = c.Boolean(false),
				IsDeleted = c.Boolean(false),
				DeleterUserId = c.Long(),
				DeletionTime = c.DateTime(),
				LastModificationTime = c.DateTime(),
				LastModifierUserId = c.Long(),
				CreationTime = c.DateTime(false),
				CreatorUserId = c.Long()
			}, new Dictionary<string, object>
			{
				{ "DynamicFilter_Role_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_Role_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			}).PrimaryKey(t => t.Id).ForeignKey("dbo.AbpUsers", t => t.CreatorUserId).ForeignKey("dbo.AbpUsers", t => t.DeleterUserId)
				.ForeignKey("dbo.AbpUsers", t => t.LastModifierUserId)
				.Index(t => t.DeleterUserId)
				.Index(t => t.LastModifierUserId)
				.Index(t => t.CreatorUserId);
			CreateTable("dbo.AbpUsers", delegate(ColumnBuilder c)
			{
				ColumnModel id = c.Long(false, identity: true);
				ColumnModel profilePictureId = c.Guid();
				ColumnModel shouldChangePasswordOnNextLogin = c.Boolean(false);
				int? maxLength = 64;
				ColumnModel authenticationSource = c.String(null, maxLength);
				ColumnModel userName = c.String(false, 32);
				ColumnModel tenantId = c.Int();
				ColumnModel emailAddress = c.String(false, 256);
				ColumnModel name = c.String(false, 32);
				ColumnModel surname = c.String(false, 32);
				ColumnModel password = c.String(false, 128);
				maxLength = 328;
				ColumnModel emailConfirmationCode = c.String(null, maxLength);
				maxLength = 328;
				return new
				{
					Id = id,
					ProfilePictureId = profilePictureId,
					ShouldChangePasswordOnNextLogin = shouldChangePasswordOnNextLogin,
					AuthenticationSource = authenticationSource,
					UserName = userName,
					TenantId = tenantId,
					EmailAddress = emailAddress,
					Name = name,
					Surname = surname,
					Password = password,
					EmailConfirmationCode = emailConfirmationCode,
					PasswordResetCode = c.String(null, maxLength),
					LockoutEndDateUtc = c.DateTime(),
					AccessFailedCount = c.Int(false),
					IsLockoutEnabled = c.Boolean(false),
					PhoneNumber = c.String(),
					IsPhoneNumberConfirmed = c.Boolean(false),
					SecurityStamp = c.String(),
					IsTwoFactorEnabled = c.Boolean(false),
					IsEmailConfirmed = c.Boolean(false),
					IsActive = c.Boolean(false),
					LastLoginTime = c.DateTime(),
					IsDeleted = c.Boolean(false),
					DeleterUserId = c.Long(),
					DeletionTime = c.DateTime(),
					LastModificationTime = c.DateTime(),
					LastModifierUserId = c.Long(),
					CreationTime = c.DateTime(false),
					CreatorUserId = c.Long()
				};
			}, new Dictionary<string, object>
			{
				{ "DynamicFilter_User_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_User_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			}).PrimaryKey(t => t.Id).ForeignKey("dbo.AbpUsers", t => t.CreatorUserId).ForeignKey("dbo.AbpUsers", t => t.DeleterUserId)
				.ForeignKey("dbo.AbpUsers", t => t.LastModifierUserId)
				.Index(t => t.DeleterUserId)
				.Index(t => t.LastModifierUserId)
				.Index(t => t.CreatorUserId);
			CreateTable("dbo.AbpUserClaims", (ColumnBuilder c) => new
			{
				Id = c.Long(false, identity: true),
				TenantId = c.Int(),
				UserId = c.Long(false),
				ClaimType = c.String(),
				ClaimValue = c.String(),
				CreationTime = c.DateTime(false),
				CreatorUserId = c.Long()
			}, new Dictionary<string, object> { { "DynamicFilter_UserClaim_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" } }).PrimaryKey(t => t.Id).ForeignKey("dbo.AbpUsers", t => t.UserId, cascadeDelete: true).Index(t => t.UserId);
			CreateTable("dbo.AbpUserLogins", (ColumnBuilder c) => new
			{
				Id = c.Long(false, identity: true),
				TenantId = c.Int(),
				UserId = c.Long(false),
				LoginProvider = c.String(false, 128),
				ProviderKey = c.String(false, 256)
			}, new Dictionary<string, object> { { "DynamicFilter_UserLogin_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" } }).PrimaryKey(t => t.Id).ForeignKey("dbo.AbpUsers", t => t.UserId, cascadeDelete: true).Index(t => t.UserId);
			CreateTable("dbo.AbpUserRoles", (ColumnBuilder c) => new
			{
				Id = c.Long(false, identity: true),
				TenantId = c.Int(),
				UserId = c.Long(false),
				RoleId = c.Int(false),
				CreationTime = c.DateTime(false),
				CreatorUserId = c.Long()
			}, new Dictionary<string, object> { { "DynamicFilter_UserRole_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" } }).PrimaryKey(t => t.Id).ForeignKey("dbo.AbpUsers", t => t.UserId, cascadeDelete: true).Index(t => t.UserId);
			CreateTable("dbo.AbpSettings", delegate(ColumnBuilder c)
			{
				ColumnModel id = c.Long(false, identity: true);
				ColumnModel tenantId = c.Int();
				ColumnModel userId = c.Long();
				ColumnModel name = c.String(false, 256);
				int? maxLength = 2000;
				return new
				{
					Id = id,
					TenantId = tenantId,
					UserId = userId,
					Name = name,
					Value = c.String(null, maxLength),
					LastModificationTime = c.DateTime(),
					LastModifierUserId = c.Long(),
					CreationTime = c.DateTime(false),
					CreatorUserId = c.Long()
				};
			}, new Dictionary<string, object> { { "DynamicFilter_Setting_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" } }).PrimaryKey(t => t.Id).ForeignKey("dbo.AbpUsers", t => t.UserId).Index(t => t.UserId);
			CreateTable("dbo.StockDocuments", delegate(ColumnBuilder c)
			{
				ColumnModel id = c.Int(false, identity: true);
				ColumnModel tenantId = c.Int(false);
				ColumnModel documentNumber = c.String(false, 32);
				ColumnModel documentType = c.String(false, 24);
				ColumnModel status = c.String(false, 24);
				ColumnModel documentDate = c.DateTime(false);
				int? maxLength = 128;
				ColumnModel partyName = c.String(null, maxLength);
				maxLength = 512;
				return new
				{
					Id = id,
					TenantId = tenantId,
					DocumentNumber = documentNumber,
					DocumentType = documentType,
					Status = status,
					DocumentDate = documentDate,
					PartyName = partyName,
					Note = c.String(null, maxLength),
					TotalAmount = c.Decimal(false, (byte)18, (byte)2),
					LinesJson = c.String(false),
					ApprovedByUserId = c.Long(),
					ApprovedTime = c.DateTime(),
					IsDeleted = c.Boolean(false),
					DeleterUserId = c.Long(),
					DeletionTime = c.DateTime(),
					LastModificationTime = c.DateTime(),
					LastModifierUserId = c.Long(),
					CreationTime = c.DateTime(false),
					CreatorUserId = c.Long()
				};
			}, new Dictionary<string, object>
			{
				{ "DynamicFilter_StockDocument_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_StockDocument_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			}).PrimaryKey(t => t.Id);
			CreateTable("dbo.StockItems", delegate(ColumnBuilder c)
			{
				ColumnModel id = c.Int(false, identity: true);
				ColumnModel tenantId = c.Int(false);
				ColumnModel code = c.String(false, 64);
				ColumnModel name = c.String(false, 128);
				int? maxLength = 32;
				return new
				{
					Id = id,
					TenantId = tenantId,
					Code = code,
					Name = name,
					Unit = c.String(null, maxLength),
					PurchasePrice = c.Decimal(false, (byte)18, (byte)2),
					SellingPrice = c.Decimal(false, (byte)18, (byte)2),
					IsDeleted = c.Boolean(false),
					DeleterUserId = c.Long(),
					DeletionTime = c.DateTime(),
					LastModificationTime = c.DateTime(),
					LastModifierUserId = c.Long(),
					CreationTime = c.DateTime(false),
					CreatorUserId = c.Long()
				};
			}, new Dictionary<string, object>
			{
				{ "DynamicFilter_StockItem_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_StockItem_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			}).PrimaryKey(t => t.Id);
			CreateTable("dbo.AbpTenantNotifications", delegate(ColumnBuilder c)
			{
				ColumnModel id = c.Guid(false);
				ColumnModel tenantId = c.Int();
				ColumnModel notificationName = c.String(false, 96);
				ColumnModel data = c.String();
				int? maxLength = 512;
				ColumnModel dataTypeName = c.String(null, maxLength);
				maxLength = 250;
				ColumnModel entityTypeName = c.String(null, maxLength);
				maxLength = 512;
				ColumnModel entityTypeAssemblyQualifiedName = c.String(null, maxLength);
				maxLength = 96;
				return new
				{
					Id = id,
					TenantId = tenantId,
					NotificationName = notificationName,
					Data = data,
					DataTypeName = dataTypeName,
					EntityTypeName = entityTypeName,
					EntityTypeAssemblyQualifiedName = entityTypeAssemblyQualifiedName,
					EntityId = c.String(null, maxLength),
					Severity = c.Byte(false),
					CreationTime = c.DateTime(false),
					CreatorUserId = c.Long()
				};
			}, new Dictionary<string, object> { { "DynamicFilter_TenantNotificationInfo_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" } }).PrimaryKey(t => t.Id);
			CreateTable("dbo.AbpTenants", delegate(ColumnBuilder c)
			{
				ColumnModel id = c.Int(false, identity: true);
				ColumnModel customCssId = c.Guid();
				ColumnModel logoId = c.Guid();
				int? maxLength = 64;
				ColumnModel logoFileType = c.String(null, maxLength);
				ColumnModel subscriptionEndDate = c.DateTime();
				ColumnModel editionId = c.Int();
				ColumnModel name = c.String(false, 128);
				ColumnModel tenancyName = c.String(false, 64);
				maxLength = 1024;
				return new
				{
					Id = id,
					CustomCssId = customCssId,
					LogoId = logoId,
					LogoFileType = logoFileType,
					SubscriptionEndDate = subscriptionEndDate,
					EditionId = editionId,
					Name = name,
					TenancyName = tenancyName,
					ConnectionString = c.String(null, maxLength),
					IsActive = c.Boolean(false),
					IsDeleted = c.Boolean(false),
					DeleterUserId = c.Long(),
					DeletionTime = c.DateTime(),
					LastModificationTime = c.DateTime(),
					LastModifierUserId = c.Long(),
					CreationTime = c.DateTime(false),
					CreatorUserId = c.Long()
				};
			}, new Dictionary<string, object> { { "DynamicFilter_Tenant_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" } }).PrimaryKey(t => t.Id).ForeignKey("dbo.AbpUsers", t => t.CreatorUserId).ForeignKey("dbo.AbpUsers", t => t.DeleterUserId)
				.ForeignKey("dbo.AbpEditions", t => t.EditionId)
				.ForeignKey("dbo.AbpUsers", t => t.LastModifierUserId)
				.Index(t => t.EditionId)
				.Index(t => t.DeleterUserId)
				.Index(t => t.LastModifierUserId)
				.Index(t => t.CreatorUserId);
			CreateTable("dbo.AbpUserAccounts", (ColumnBuilder c) => new
			{
				Id = c.Long(false, identity: true),
				TenantId = c.Int(),
				UserId = c.Long(false),
				UserLinkId = c.Long(),
				UserName = c.String(),
				EmailAddress = c.String(),
				LastLoginTime = c.DateTime(),
				IsDeleted = c.Boolean(false),
				DeleterUserId = c.Long(),
				DeletionTime = c.DateTime(),
				LastModificationTime = c.DateTime(),
				LastModifierUserId = c.Long(),
				CreationTime = c.DateTime(false),
				CreatorUserId = c.Long()
			}, new Dictionary<string, object> { { "DynamicFilter_UserAccount_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" } }).PrimaryKey(t => t.Id);
			CreateTable("dbo.AbpUserLoginAttempts", delegate(ColumnBuilder c)
			{
				ColumnModel id = c.Long(false, identity: true);
				ColumnModel tenantId = c.Int();
				int? maxLength = 64;
				ColumnModel tenancyName = c.String(null, maxLength);
				ColumnModel userId = c.Long();
				maxLength = 255;
				ColumnModel userNameOrEmailAddress = c.String(null, maxLength);
				maxLength = 64;
				ColumnModel clientIpAddress = c.String(null, maxLength);
				maxLength = 128;
				ColumnModel clientName = c.String(null, maxLength);
				maxLength = 256;
				return new
				{
					Id = id,
					TenantId = tenantId,
					TenancyName = tenancyName,
					UserId = userId,
					UserNameOrEmailAddress = userNameOrEmailAddress,
					ClientIpAddress = clientIpAddress,
					ClientName = clientName,
					BrowserInfo = c.String(null, maxLength),
					Result = c.Byte(false),
					CreationTime = c.DateTime(false)
				};
			}, new Dictionary<string, object> { { "DynamicFilter_UserLoginAttempt_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" } }).PrimaryKey(t => t.Id).Index(t => new { t.UserId, t.TenantId }).Index(t => new { t.TenancyName, t.UserNameOrEmailAddress, t.Result });
			CreateTable("dbo.AbpUserNotifications", (ColumnBuilder c) => new
			{
				Id = c.Guid(false),
				TenantId = c.Int(),
				UserId = c.Long(false),
				TenantNotificationId = c.Guid(false),
				State = c.Int(false),
				CreationTime = c.DateTime(false)
			}, new Dictionary<string, object> { { "DynamicFilter_UserNotificationInfo_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" } }).PrimaryKey(t => t.Id).Index(t => new { t.UserId, t.State, t.CreationTime });
			CreateTable("dbo.AbpUserOrganizationUnits", (ColumnBuilder c) => new
			{
				Id = c.Long(false, identity: true),
				TenantId = c.Int(),
				UserId = c.Long(false),
				OrganizationUnitId = c.Long(false),
				IsDeleted = c.Boolean(false),
				CreationTime = c.DateTime(false),
				CreatorUserId = c.Long()
			}, new Dictionary<string, object>
			{
				{ "DynamicFilter_UserOrganizationUnit_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_UserOrganizationUnit_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			}).PrimaryKey(t => t.Id);
		}

		public override void Down()
		{
			DropForeignKey("dbo.AbpTenants", "LastModifierUserId", "dbo.AbpUsers");
			DropForeignKey("dbo.AbpTenants", "EditionId", "dbo.AbpEditions");
			DropForeignKey("dbo.AbpTenants", "DeleterUserId", "dbo.AbpUsers");
			DropForeignKey("dbo.AbpTenants", "CreatorUserId", "dbo.AbpUsers");
			DropForeignKey("dbo.AbpPermissions", "RoleId", "dbo.AbpRoles");
			DropForeignKey("dbo.AbpRoles", "LastModifierUserId", "dbo.AbpUsers");
			DropForeignKey("dbo.AbpRoles", "DeleterUserId", "dbo.AbpUsers");
			DropForeignKey("dbo.AbpRoles", "CreatorUserId", "dbo.AbpUsers");
			DropForeignKey("dbo.AbpSettings", "UserId", "dbo.AbpUsers");
			DropForeignKey("dbo.AbpUserRoles", "UserId", "dbo.AbpUsers");
			DropForeignKey("dbo.AbpPermissions", "UserId", "dbo.AbpUsers");
			DropForeignKey("dbo.AbpUserLogins", "UserId", "dbo.AbpUsers");
			DropForeignKey("dbo.AbpUsers", "LastModifierUserId", "dbo.AbpUsers");
			DropForeignKey("dbo.AbpUsers", "DeleterUserId", "dbo.AbpUsers");
			DropForeignKey("dbo.AbpUsers", "CreatorUserId", "dbo.AbpUsers");
			DropForeignKey("dbo.AbpUserClaims", "UserId", "dbo.AbpUsers");
			DropForeignKey("dbo.AbpOrganizationUnits", "ParentId", "dbo.AbpOrganizationUnits");
			DropForeignKey("dbo.AbpFeatures", "EditionId", "dbo.AbpEditions");
			DropIndex("dbo.AbpUserNotifications", new string[3] { "UserId", "State", "CreationTime" });
			DropIndex("dbo.AbpUserLoginAttempts", new string[3] { "TenancyName", "UserNameOrEmailAddress", "Result" });
			DropIndex("dbo.AbpUserLoginAttempts", new string[2] { "UserId", "TenantId" });
			DropIndex("dbo.AbpTenants", new string[1] { "CreatorUserId" });
			DropIndex("dbo.AbpTenants", new string[1] { "LastModifierUserId" });
			DropIndex("dbo.AbpTenants", new string[1] { "DeleterUserId" });
			DropIndex("dbo.AbpTenants", new string[1] { "EditionId" });
			DropIndex("dbo.AbpSettings", new string[1] { "UserId" });
			DropIndex("dbo.AbpUserRoles", new string[1] { "UserId" });
			DropIndex("dbo.AbpUserLogins", new string[1] { "UserId" });
			DropIndex("dbo.AbpUserClaims", new string[1] { "UserId" });
			DropIndex("dbo.AbpUsers", new string[1] { "CreatorUserId" });
			DropIndex("dbo.AbpUsers", new string[1] { "LastModifierUserId" });
			DropIndex("dbo.AbpUsers", new string[1] { "DeleterUserId" });
			DropIndex("dbo.AbpRoles", new string[1] { "CreatorUserId" });
			DropIndex("dbo.AbpRoles", new string[1] { "LastModifierUserId" });
			DropIndex("dbo.AbpRoles", new string[1] { "DeleterUserId" });
			DropIndex("dbo.AbpPermissions", new string[1] { "UserId" });
			DropIndex("dbo.AbpPermissions", new string[1] { "RoleId" });
			DropIndex("dbo.AbpOrganizationUnits", new string[1] { "ParentId" });
			DropIndex("dbo.AbpNotificationSubscriptions", new string[4] { "NotificationName", "EntityTypeName", "EntityId", "UserId" });
			DropIndex("dbo.AbpFeatures", new string[1] { "EditionId" });
			DropIndex("dbo.AbpBackgroundJobs", new string[2] { "IsAbandoned", "NextTryTime" });
			DropTable("dbo.AbpUserOrganizationUnits", new Dictionary<string, object>
			{
				{ "DynamicFilter_UserOrganizationUnit_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_UserOrganizationUnit_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			});
			DropTable("dbo.AbpUserNotifications", new Dictionary<string, object> { { "DynamicFilter_UserNotificationInfo_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" } });
			DropTable("dbo.AbpUserLoginAttempts", new Dictionary<string, object> { { "DynamicFilter_UserLoginAttempt_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" } });
			DropTable("dbo.AbpUserAccounts", new Dictionary<string, object> { { "DynamicFilter_UserAccount_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" } });
			DropTable("dbo.AbpTenants", new Dictionary<string, object> { { "DynamicFilter_Tenant_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" } });
			DropTable("dbo.AbpTenantNotifications", new Dictionary<string, object> { { "DynamicFilter_TenantNotificationInfo_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" } });
			DropTable("dbo.StockItems", new Dictionary<string, object>
			{
				{ "DynamicFilter_StockItem_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_StockItem_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			});
			DropTable("dbo.StockDocuments", new Dictionary<string, object>
			{
				{ "DynamicFilter_StockDocument_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_StockDocument_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			});
			DropTable("dbo.AbpSettings", new Dictionary<string, object> { { "DynamicFilter_Setting_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" } });
			DropTable("dbo.AbpUserRoles", new Dictionary<string, object> { { "DynamicFilter_UserRole_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" } });
			DropTable("dbo.AbpUserLogins", new Dictionary<string, object> { { "DynamicFilter_UserLogin_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" } });
			DropTable("dbo.AbpUserClaims", new Dictionary<string, object> { { "DynamicFilter_UserClaim_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" } });
			DropTable("dbo.AbpUsers", new Dictionary<string, object>
			{
				{ "DynamicFilter_User_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_User_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			});
			DropTable("dbo.AbpRoles", new Dictionary<string, object>
			{
				{ "DynamicFilter_Role_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_Role_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			});
			DropTable("dbo.AbpPermissions", new Dictionary<string, object>
			{
				{ "DynamicFilter_PermissionSetting_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_RolePermissionSetting_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_UserPermissionSetting_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			});
			DropTable("dbo.AbpOrganizationUnits", new Dictionary<string, object>
			{
				{ "DynamicFilter_OrganizationUnit_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_OrganizationUnit_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			});
			DropTable("dbo.AbpNotificationSubscriptions", new Dictionary<string, object> { { "DynamicFilter_NotificationSubscriptionInfo_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" } });
			DropTable("dbo.AbpNotifications");
			DropTable("dbo.AbpLanguageTexts", new Dictionary<string, object> { { "DynamicFilter_ApplicationLanguageText_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" } });
			DropTable("dbo.AbpLanguages", new Dictionary<string, object>
			{
				{ "DynamicFilter_ApplicationLanguage_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" },
				{ "DynamicFilter_ApplicationLanguage_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" }
			});
			DropTable("dbo.AbpEditions", new Dictionary<string, object> { { "DynamicFilter_Edition_SoftDelete", "EntityFramework.DynamicFilters.DynamicFilterDefinition" } });
			DropTable("dbo.AbpFeatures", new Dictionary<string, object> { { "DynamicFilter_TenantFeatureSetting_MustHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" } });
			DropTable("dbo.AppBinaryObjects", new Dictionary<string, object> { { "DynamicFilter_BinaryObject_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" } });
			DropTable("dbo.AbpBackgroundJobs");
			DropTable("dbo.AbpAuditLogs", new Dictionary<string, object> { { "DynamicFilter_AuditLog_MayHaveTenant", "EntityFramework.DynamicFilters.DynamicFilterDefinition" } });
		}
	}
}
