using System.Web.Optimization;
using StockPilot.Web.Bundling;

namespace StockPilot.Web.App.Startup
{
	public static class AppBundleConfig
	{
		public static void RegisterBundles(BundleCollection bundles)
		{
			AddAppCssLibs(bundles, isRTL: false);
			AddAppCssLibs(bundles, isRTL: true);
			bundles.Add(new ScriptBundle("~/Bundles/App/libs/js").Include("~/libs/json2/json2.min.js", "~/libs/jquery/jquery.min.js", "~/libs/jquery-ui/jquery-ui.min.js", "~/libs/jquery/jquery-migrate.min.js", "~/libs/bootstrap/js/bootstrap.min.js", "~/libs/bootstrap-hover-dropdown/bootstrap-hover-dropdown.min.js", "~/libs/jquery-slimscroll/jquery.slimscroll.min.js", "~/libs/jquery-blockui/jquery.blockui.min.js", "~/libs/js-cookie/js.cookie.min.js", "~/libs/jquery-uniform/jquery.uniform.min.js", "~/Scripts/jquery.signalR-2.2.2.min.js", "~/Scripts/localforage/localforage.min.js", "~/libs/morris/morris.min.js", "~/libs/morris/raphael-min.js", "~/libs/jquery-sparkline/jquery.sparkline.min.js", "~/libs/jcrop/js/jquery.color.js", "~/libs/jcrop/js/jquery.Jcrop.min.js", "~/libs/jquery-timeago/jquery.timeago.js", "~/libs/jstree/jstree.js", "~/libs/bootstrap-switch/js/bootstrap-switch.min.js", "~/libs/spinjs/spin.js", "~/libs/spinjs/jquery.spin.js", "~/libs/sweetalert/sweet-alert.min.js", "~/libs/push-js/push.min.js", "~/libs/toastr/toastr.min.js", "~/Scripts/moment-with-locales.min.js", "~/Scripts/moment-timezone-with-data.min.js", "~/libs/bootstrap-daterangepicker/daterangepicker.js", "~/libs/bootstrap-select/bootstrap-select.min.js", "~/Scripts/underscore.min.js", "~/Scripts/angular.js", "~/Scripts/angular-sanitize.min.js", "~/Scripts/angular-touch.min.js", "~/Scripts/angular-ui-router.min.js", "~/Scripts/angular-ui/ui-utils.min.js", "~/Scripts/angular-ui/ui-bootstrap-tpls.min.js", "~/libs/angular-ui-grid/ui-grid.min.js", "~/libs/ui-select/select.min.js", "~/libs/angular-ocLazyLoad/ocLazyLoad.min.js", "~/libs/angular-file-upload/angular-file-upload.min.js", "~/libs/angular-daterangepicker/angular-daterangepicker.min.js", "~/libs/angular-moment/angular-moment.min.js", "~/libs/angular-bootstrap-switch/angular-bootstrap-switch.min.js", "~/libs/datePicker/datePicker.js", "~/libs/ng-tags-input/ng-tags-input.js", "~/Abp/Framework/scripts/abp.js", "~/Abp/Framework/scripts/libs/abp.jquery.js", "~/Abp/Framework/scripts/libs/abp.toastr.js", "~/Abp/Framework/scripts/libs/abp.blockUI.js", "~/Abp/Framework/scripts/libs/abp.spin.js", "~/Abp/Framework/scripts/libs/abp.sweet-alert.js", "~/Abp/Framework/scripts/libs/abp.moment.js", "~/Abp/Framework/scripts/libs/angularjs/abp.ng.js", "~/libs/angular-gridster/angular-gridster.js", "~/libs/quill/quill.js", "~/libs/quill/ng-quill.js", "~/libs/barcode/JsBarcode.all.min.js").ForceOrdered());
			AddAppMetronicCss(bundles, isRTL: false);
			AddAppMetronicCss(bundles, isRTL: true);
			bundles.Add(new ScriptBundle("~/Bundles/App/metronic/js").Include("~/metronic/assets/global/scripts/app.js", "~/metronic/assets/admin/layout4/scripts/layout.js", "~/metronic/assets/layouts/global/scripts/quick-sidebar.js").ForceOrdered());
			bundles.Add(new StyleBundle("~/Bundles/App/css").IncludeDirectory("~/App", "*.css", searchSubdirectories: true).ForceOrdered());
			bundles.Add(new ScriptBundle("~/Bundles/App/js").IncludeDirectory("~/App", "*.js", searchSubdirectories: true).ForceOrdered());
		}

		private static void AddAppCssLibs(BundleCollection bundles, bool isRTL)
		{
			bundles.Add(new StyleBundle("~/Bundles/App/libs/css" + (isRTL ? "RTL" : "")).Include("~/Content/font-awesome.min.css", new CssRewriteUrlWithVirtualDirectoryTransform()).Include("~/libs/simple-line-icons/simple-line-icons.min.css", new CssRewriteUrlWithVirtualDirectoryTransform()).Include("~/Content/flags/famfamfam-flags.css", new CssRewriteUrlWithVirtualDirectoryTransform())
				.Include(isRTL ? "~/libs/bootstrap/css/bootstrap-rtl.min.css" : "~/libs/bootstrap/css/bootstrap.min.css", new CssRewriteUrlWithVirtualDirectoryTransform())
				.Include("~/libs/jquery-uniform/css/uniform.default.css", new CssRewriteUrlWithVirtualDirectoryTransform())
				.Include("~/libs/morris/morris.css")
				.Include("~/libs/jquery-ui/jquery-ui.min.css")
				.Include("~/libs/jstree/themes/default/style.css", new CssRewriteUrlWithVirtualDirectoryTransform())
				.Include("~/libs/sweetalert/sweet-alert.css")
				.Include("~/libs/toastr/toastr.min.css")
				.Include("~/libs/angular-ui-grid/ui-grid.min.css", new CssRewriteUrlWithVirtualDirectoryTransform())
				.Include("~/libs/ui-select/select.min.css", new CssRewriteUrlWithVirtualDirectoryTransform())
				.Include("~/libs/bootstrap-daterangepicker/daterangepicker.css")
				.Include("~/libs/bootstrap-select/bootstrap-select.min.css")
				.Include("~/libs/bootstrap-switch/css/bootstrap-switch.min.css")
				.Include("~/libs/jcrop/css/jquery.Jcrop.min.css")
				.Include("~/libs/angular-gridster/angular-gridster.min.css")
				.Include("~/libs/quill/quill.snow.css")
				.Include("~/libs/quill/quill.bubble.css")
				.Include("~/libs/ng-tags-input/ng-tags-input.css")
				.ForceOrdered());
		}

		private static void AddAppMetronicCss(BundleCollection bundles, bool isRTL)
		{
			bundles.Add(new StyleBundle("~/Bundles/App/metronic/css" + (isRTL ? "RTL" : "")).Include("~/metronic/assets/global/css/components-md" + (isRTL ? "-rtl" : "") + ".css", new CssRewriteUrlWithVirtualDirectoryTransform()).Include("~/metronic/assets/global/css/plugins-md" + (isRTL ? "-rtl" : "") + ".css", new CssRewriteUrlWithVirtualDirectoryTransform()).Include("~/metronic/assets/admin/layout4/css/layout" + (isRTL ? "-rtl" : "") + ".css", new CssRewriteUrlWithVirtualDirectoryTransform())
				.Include("~/metronic/assets/admin/layout4/css/themes/light" + (isRTL ? "-rtl" : "") + ".css", new CssRewriteUrlWithVirtualDirectoryTransform())
				.ForceOrdered());
		}
	}
}
