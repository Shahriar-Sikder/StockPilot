using System;
using System.Reflection;
using Abp.AutoMapper;
using Abp.Dependency;
using Abp.Localization.Dictionaries;
using Abp.Localization.Dictionaries.Xml;
using Abp.Modules;
using Abp.Net.Mail;
using Abp.Net.Mail.Smtp;
using Abp.Zero;
using Abp.Zero.Configuration;
using Abp.Zero.Ldap;
using Castle.MicroKernel.Registration;
using StockPilot.Authorization.Roles;
using StockPilot.Authorization.Users;
using StockPilot.Configuration;
using StockPilot.Debugging;
using StockPilot.Emailing;
using StockPilot.Features;
using StockPilot.MultiTenancy;
using StockPilot.Notifications;

namespace StockPilot
{
	[DependsOn(new Type[]
	{
		typeof(AbpZeroCoreModule),
		typeof(AbpZeroLdapModule),
		typeof(AbpAutoMapperModule)
	})]
	public class StockPilotCoreModule : AbpModule
	{
		public override void PreInitialize()
		{
			base.Configuration.Auditing.IsEnabledForAnonymousUsers = true;
			base.Configuration.Modules.Zero().EntityTypes.Tenant = typeof(Tenant);
			base.Configuration.Modules.Zero().EntityTypes.Role = typeof(Role);
			base.Configuration.Modules.Zero().EntityTypes.User = typeof(User);
			base.Configuration.Localization.Sources.Add(new DictionaryBasedLocalizationSource("StockPilot", new XmlEmbeddedFileLocalizationDictionaryProvider(Assembly.GetExecutingAssembly(), "StockPilot.Localization.StockPilot")));
			base.Configuration.Features.Providers.Add<AppFeatureProvider>();
			base.Configuration.Settings.Providers.Add<AppSettingProvider>();
			base.Configuration.Notifications.Providers.Add<AppNotificationProvider>();
			base.Configuration.MultiTenancy.IsEnabled = true;
			AppRoleConfig.Configure(base.Configuration.Modules.Zero().RoleManagement);
			if (DebugHelper.IsDebug)
			{
				base.IocManager.Register<IEmailSender, NullEmailSender>(DependencyLifeStyle.Transient);
			}
			base.Configuration.ReplaceService(typeof(IEmailSenderConfiguration), delegate
			{
				base.Configuration.IocManager.IocContainer.Register(Component.For<IEmailSenderConfiguration, ISmtpEmailSenderConfiguration>().ImplementedBy<StockPilotSmtpEmailSenderConfiguration>().LifestyleTransient());
			});
		}

		public override void Initialize()
		{
			base.IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());
		}

		public override void PostInitialize()
		{
		}
	}
}
