namespace StockPilot.Entity
{
	public enum DocumentStatus
	{
		Draft,
		Posted,
		Cancelled
	}

	public enum IssueSaleType
	{
		Sale = 1,
		InternalIssue,
		Damage,
		Sample
	}

	public enum StockMovementType
	{
		Opening = 1,
		PurchaseIn,
		PurchaseReturnOut,
		SaleOut,
		SaleReturnIn,
		IssueOut,
		AdjustmentIn,
		AdjustmentOut,
		TransferIn,
		TransferOut
	}

}
