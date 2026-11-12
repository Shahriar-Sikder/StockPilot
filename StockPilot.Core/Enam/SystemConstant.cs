namespace StockPilot.Enam
{
	public static class SystemConstant
	{
		public enum EntityTypeName
		{
			UnitSet,
			CategoryType,
			RequsitionType,
			ReturnType,
			ReveiveType,
			PurchaseOrderType,
			CommonStatus,
			CostingStatus,
			ProjectStatus,
			RequsitionStatus,
			PurchaseOrderStatus,
			ExpenseType,
			POSPrint,
			OrderStatus
		}

		public enum StockInFrom
		{
			Sales,
			Purchase,
			Return
		}

		public enum CashBoxTransactionType
		{
			CashIn,
			CashOut
		}

		public enum CapitalTransactionType
		{
			Invest,
			Withdraw
		}

		public enum EcommDescriptionType
		{
			TermNCondition,
			HowToApply,
			PrivacyPolicy,
			AboutUs,
			ContactUs,
			ReturnNExchanges,
			SecureShopping
		}
	}
}
