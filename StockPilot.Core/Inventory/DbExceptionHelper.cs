using System;

namespace StockPilot.Inventory
{
	internal static class DbExceptionHelper
	{
		public static bool IsSaveConflict(Exception exception)
		{
			for (Exception current = exception; current != null; current = current.InnerException)
			{
				switch (current.GetType().Name)
				{
				case "DbUpdateConcurrencyException":
				case "OptimisticConcurrencyException":
				case "AbpDbConcurrencyException":
				case "DbUpdateException":
					return true;
				}
			}
			return false;
		}
	}
}
