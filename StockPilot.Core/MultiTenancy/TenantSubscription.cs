using System;

namespace StockPilot.MultiTenancy
{
	public static class TenantSubscription
	{
		public static TenantSubscriptionStatus GetStatus(DateTime? endDate, DateTime today, int warnBeforeDays)
		{
			if (!endDate.HasValue)
			{
				return TenantSubscriptionStatus.None;
			}
			DateTime end = endDate.Value.Date;
			if (end < today.Date)
			{
				return TenantSubscriptionStatus.Expired;
			}
			if (!(end <= today.Date.AddDays(Math.Max(0, warnBeforeDays))))
			{
				return TenantSubscriptionStatus.Active;
			}
			return TenantSubscriptionStatus.ExpiringSoon;
		}

		public static bool IsExpired(DateTime? endDate, DateTime today)
		{
			if (endDate.HasValue)
			{
				return endDate.Value.Date < today.Date;
			}
			return false;
		}

		public static bool IsLoginBlocked(DateTime? endDate, DateTime today, int graceDays)
		{
			if (endDate.HasValue)
			{
				return today.Date > endDate.Value.Date.AddDays(Math.Max(0, graceDays));
			}
			return false;
		}
	}
}
