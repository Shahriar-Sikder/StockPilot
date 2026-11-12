using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.Description;
using Abp.Application.Services;
using Abp.Configuration.Startup;
using Abp.Modules;
using Abp.WebApi;
using Swashbuckle.Application;

namespace StockPilot.WebApi
{
	[DependsOn(new Type[]
	{
		typeof(AbpWebApiModule),
		typeof(StockPilotApplicationModule)
	})]
	public class StockPilotWebApiModule : AbpModule
	{
		public override void Initialize()
		{
			base.IocManager.RegisterAssemblyByConvention(Assembly.GetExecutingAssembly());
			base.Configuration.Modules.AbpWebApi().DynamicApiControllerBuilder.ForAll<IApplicationService>(typeof(StockPilotApplicationModule).Assembly, "app").Build();
			base.Configuration.Modules.AbpWebApi().HttpConfiguration.Filters.Add(new HostAuthenticationFilter("Bearer"));
			ConfigureSwaggerUi();
		}

		private void ConfigureSwaggerUi()
		{
			base.Configuration.Modules.AbpWebApi().HttpConfiguration.EnableSwagger(delegate(SwaggerDocsConfig c)
			{
				c.SingleApiVersion("v1", "StockPilot.WebApi");
				c.ResolveConflictingActions((IEnumerable<ApiDescription> apiDescriptions) => apiDescriptions.First());
			}).EnableSwaggerUi(delegate(SwaggerUiConfig c)
			{
				c.InjectJavaScript(Assembly.GetAssembly(typeof(StockPilotWebApiModule)), "StockPilot.WebApi.Scripts.Swagger-Custom.js");
			});
		}
	}
}
