using System.Web.Mvc;
using Abp.Auditing;

namespace StockPilot.Web.Controllers
{
	public class ErrorController : StockPilotControllerBase
	{
		[DisableAuditing]
		public ActionResult E404()
		{
			return Redirect(base.Url.Content("~/react/index.html#/error"));
		}
	}
}
