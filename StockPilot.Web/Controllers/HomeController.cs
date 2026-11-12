using System.Web.Mvc;

namespace StockPilot.Web.Controllers
{
	public class HomeController : StockPilotControllerBase
	{
		public ActionResult Index()
		{
			return Redirect(base.Url.Content("~/react/index.html#/landing"));
		}
	}
}
