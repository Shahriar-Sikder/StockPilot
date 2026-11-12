using System.Web.Optimization;

namespace StockPilot.Web.Bundling
{
	public static class CommonBundleConfig
	{
		public static void RegisterBundles(BundleCollection bundles)
		{
			bundles.Add(new StyleBundle("~/Bundles/Common/css").IncludeDirectory("~/Common/Styles", "*.css", searchSubdirectories: true).ForceOrdered());
			bundles.Add(new ScriptBundle("~/Bundles/Common/js").IncludeDirectory("~/Common/Scripts", "*.js", searchSubdirectories: true).ForceOrdered());
		}
	}
}
