using Abp.Dependency;
using Abp.Runtime.Session;
using Abp.Web.Mvc.Views;

namespace StockPilot.Web.Views
{
	public abstract class StockPilotWebViewPageBase : StockPilotWebViewPageBase<dynamic>
	{
	}
	public abstract class StockPilotWebViewPageBase<TModel> : AbpWebViewPage<TModel>
	{
		public IAbpSession AbpSession { get; private set; }

		protected StockPilotWebViewPageBase()
		{
			AbpSession = IocManager.Instance.Resolve<IAbpSession>();
			base.LocalizationSourceName = "StockPilot";
		}
	}
}
