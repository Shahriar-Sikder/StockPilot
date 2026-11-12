using System;
using System.Data.Entity;
using System.Reflection;
using Abp.Modules;
using Abp.Zero.EntityFramework;
using StockPilot.EntityFramework;

namespace StockPilot
{
	[DependsOn(new Type[]
	{
		typeof(AbpZeroEntityFrameworkModule),
		typeof(StockPilotCoreModule)
	})]
	public class StockPilotDataModule : AbpModule
	{
		public override void PreInitialize()
		{
			Database.SetInitializer(new StockPilotDatabaseInitializer());
			base.Configuration.DefaultNameOrConnectionString = "Default";
		}

		public override void Initialize()
		{
			base.IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());
		}
	}
}
