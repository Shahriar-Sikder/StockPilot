using System;
using System.Collections.Generic;
using System.Linq;
using StockPilot.Entity;

namespace StockPilot.Inventory
{
	public static class DocumentTotals
	{
		public static void CalculateLine(PurchaseInvoiceItem line, int decimals)
		{
			decimal gross = line.Quantity * line.UnitCost;
			line.DiscountAmount = GetDiscount(gross, line.DiscountPercent, line.DiscountAmount, decimals);
			line.TaxAmount = Round((gross - line.DiscountAmount) * line.TaxPercent / 100m, decimals);
			line.LineTotal = Round(gross - line.DiscountAmount + line.TaxAmount, decimals);
		}

		public static void CalculateHeader(PurchaseInvoice invoice, IEnumerable<PurchaseInvoiceItem> lines, int decimals)
		{
			List<PurchaseInvoiceItem> list = lines.ToList();
			invoice.SubTotal = Round(list.Sum((PurchaseInvoiceItem l) => l.Quantity * l.UnitCost), decimals);
			invoice.DiscountAmount = list.Sum((PurchaseInvoiceItem l) => l.DiscountAmount);
			invoice.TaxAmount = list.Sum((PurchaseInvoiceItem l) => l.TaxAmount);
			invoice.OtherCharges = Round(invoice.OtherCharges, decimals);
			invoice.TotalAmount = invoice.SubTotal - invoice.DiscountAmount + invoice.TaxAmount + invoice.OtherCharges;
		}

		public static void CalculateLine(IssueSalesItem line, int decimals)
		{
			decimal gross = line.Quantity * line.UnitPrice;
			line.DiscountAmount = GetDiscount(gross, line.DiscountPercent, line.DiscountAmount, decimals);
			line.LineTotal = Round(gross - line.DiscountAmount, decimals);
		}

		public static void CalculateHeader(IssueSale document, IEnumerable<IssueSalesItem> lines, int decimals)
		{
			List<IssueSalesItem> list = lines.ToList();
			document.SubTotal = Round(list.Sum((IssueSalesItem l) => l.Quantity * l.UnitPrice), decimals);
			document.DiscountAmount = list.Sum((IssueSalesItem l) => l.DiscountAmount);
			document.TaxAmount = Round(document.TaxAmount, decimals);
			document.TotalAmount = document.SubTotal - document.DiscountAmount + document.TaxAmount;
		}

		public static bool IsDiscountValid(decimal gross, decimal discountAmount)
		{
			if (discountAmount >= 0m)
			{
				return discountAmount <= gross;
			}
			return false;
		}

		public static decimal Round(decimal value, int decimals)
		{
			return Math.Round(value, Math.Max(0, Math.Min(decimals, 4)), MidpointRounding.AwayFromZero);
		}

		private static decimal GetDiscount(decimal gross, decimal percent, decimal amount, int decimals)
		{
			if (!(percent > 0m))
			{
				return Round(amount, decimals);
			}
			return Round(gross * percent / 100m, decimals);
		}
	}
}
