using System;
using System.Reflection;
using Abp.AutoMapper;
using Abp.Modules;
using AutoMapper;
using StockPilot.Authorization;

namespace StockPilot
{
	[DependsOn(new Type[] { typeof(StockPilotCoreModule) })]
	public class StockPilotApplicationModule : AbpModule
	{
		public override void PreInitialize()
		{
			base.Configuration.Authorization.Providers.Add<AppAuthorizationProvider>();
			base.Configuration.Modules.AbpAutoMapper().Configurators.Add(delegate(IMapperConfigurationExpression mapper)
			{
				CustomDtoMapper.CreateMappings(mapper);
			});
		}

		public override void Initialize()
		{
			base.IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());
		}
	}
}
