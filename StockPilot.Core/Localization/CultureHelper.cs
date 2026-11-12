using System.Threading;

namespace StockPilot.Localization
{
	public static class CultureHelper
	{
		public static bool IsRtl => Thread.CurrentThread.CurrentUICulture.TextInfo.IsRightToLeft;
	}
}
