using System;
using System.Collections.Generic;

namespace StockPilot.Services.InventoryReport.Dtos
{
	public class InvoiceToSalesRowDto
	{
		public int InvoiceId { get; set; }

		public string InvoiceNo { get; set; }

		public DateTime InvoiceDate { get; set; }

		public string SupplierName { get; set; }

		public int ItemId { get; set; }

		public string ItemCode { get; set; }

		public string ItemName { get; set; }

		public string BaseUnitCode { get; set; }

		public decimal ReceivedQuantity { get; set; }

		public decimal UnitCost { get; set; }

		public decimal ReturnedQuantity { get; set; }

		public decimal SoldQuantity { get; set; }

		public decimal IssuedQuantity { get; set; }

		public decimal OtherOutQuantity { get; set; }

		public decimal RemainingQuantity { get; set; }

		public decimal CostOfSold { get; set; }

		public decimal CurrentSalePrice { get; set; }

		public decimal SalesValue { get; set; }

		public decimal Margin { get; set; }

		public decimal? MarginPercent { get; set; }

		public List<InvoiceToSalesAllocationDto> Allocations { get; set; } = new List<InvoiceToSalesAllocationDto>();
	}
}
