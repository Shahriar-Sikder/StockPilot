using System.Web.Mvc;

namespace StockPilot.Web.Controllers
{
	public class AboutController : StockPilotControllerBase
	{
		public ActionResult Index()
		{
			return Redirect(base.Url.Content("~/react/index.html#/about"));
		}
	}
}
