using System;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using StockPilot.Entity;
using StockPilot.Storage;

namespace StockPilot.EntityFramework
{
	public static class StockPilotDemoSeeder
	{
		public const string MockupTenantName = "Acme Traders";

		public static bool IsEnabledFor(string tenantName)
		{
			if (tenantName == "Acme Traders")
			{
				return string.Equals(ConfigurationManager.AppSettings["StockPilot:EnableDemoSeed"], "true", StringComparison.OrdinalIgnoreCase);
			}
			return false;
		}

		public static void Seed(StockPilotDbContext context, int tenantId)
		{
			if (!context.ItemMasters.Any((ItemMaster x) => x.TenantId == tenantId))
			{
				Country[] countries = new Country[3]
				{
					new Country
					{
						TenantId = tenantId,
						Code = "BD",
						Name = "Bangladesh",
						PhoneCode = "+880"
					},
					new Country
					{
						TenantId = tenantId,
						Code = "CN",
						Name = "China",
						PhoneCode = "+86"
					},
					new Country
					{
						TenantId = tenantId,
						Code = "IN",
						Name = "India",
						PhoneCode = "+91"
					}
				};
				context.Countries.AddRange(countries);
				Currency[] currencies = new Currency[2]
				{
					new Currency
					{
						TenantId = tenantId,
						Code = "BDT",
						Name = "Bangladeshi Taka",
						Symbol = "৳",
						DecimalPlaces = 2,
						IsBaseCurrency = true
					},
					new Currency
					{
						TenantId = tenantId,
						Code = "USD",
						Name = "US Dollar",
						Symbol = "$",
						DecimalPlaces = 2
					}
				};
				context.Currencies.AddRange(currencies);
				ItemCategory[] categories = new ItemCategory[2]
				{
					new ItemCategory
					{
						TenantId = tenantId,
						Code = "ELE",
						Name = "Electronics",
						PictureId = AddTile(context, tenantId, "\ud83d\udcbb", "#dbeafe")
					},
					new ItemCategory
					{
						TenantId = tenantId,
						Code = "GRO",
						Name = "Grocery",
						PictureId = AddTile(context, tenantId, "\ud83d\uded2", "#dcfce7")
					}
				};
				context.ItemCategories.AddRange(categories);
				UnitSet[] sets = new UnitSet[2]
				{
					new UnitSet
					{
						TenantId = tenantId,
						Code = "CNT",
						Name = "Count"
					},
					new UnitSet
					{
						TenantId = tenantId,
						Code = "WGT",
						Name = "Weight"
					}
				};
				context.Set<UnitSet>().AddRange(sets);
				context.SaveChanges();
				ItemSubCategory[] subs = new ItemSubCategory[3]
				{
					new ItemSubCategory
					{
						TenantId = tenantId,
						ItemCategoryId = categories[0].Id,
						Code = "MOB",
						Name = "Mobile Phones",
						PictureId = AddTile(context, tenantId, "\ud83d\udcf1", "#dbeafe")
					},
					new ItemSubCategory
					{
						TenantId = tenantId,
						ItemCategoryId = categories[0].Id,
						Code = "ACC",
						Name = "Accessories",
						PictureId = AddTile(context, tenantId, "\ud83c\udfa7", "#ede9fe")
					},
					new ItemSubCategory
					{
						TenantId = tenantId,
						ItemCategoryId = categories[1].Id,
						Code = "BEV",
						Name = "Beverages",
						PictureId = AddTile(context, tenantId, "\ud83e\udd64", "#ffedd5")
					}
				};
				context.ItemSubCategories.AddRange(subs);
				UnitMaster[] units = new UnitMaster[4]
				{
					new UnitMaster
					{
						TenantId = tenantId,
						UnitSetId = sets[0].Id,
						Code = "PCS",
						Name = "Piece",
						IsBaseUnit = true
					},
					new UnitMaster
					{
						TenantId = tenantId,
						UnitSetId = sets[0].Id,
						Code = "BOX",
						Name = "Box of 12"
					},
					new UnitMaster
					{
						TenantId = tenantId,
						UnitSetId = sets[1].Id,
						Code = "KG",
						Name = "Kilogram",
						IsBaseUnit = true
					},
					new UnitMaster
					{
						TenantId = tenantId,
						UnitSetId = sets[1].Id,
						Code = "G",
						Name = "Gram"
					}
				};
				context.UnitMasters.AddRange(units);
				Supplier[] suppliers = new Supplier[2]
				{
					new Supplier
					{
						TenantId = tenantId,
						Code = "SUP-001",
						Name = "Dhaka Gadgets Ltd",
						ContactPerson = "Rahim Uddin",
						Phone = "01711-000111",
						CountryId = countries[0].Id,
						CurrencyId = currencies[0].Id,
						PaymentTermDays = 30
					},
					new Supplier
					{
						TenantId = tenantId,
						Code = "SUP-002",
						Name = "Sylhet Tea Co",
						ContactPerson = "Karim Ali",
						Phone = "01811-000222",
						CountryId = countries[0].Id,
						CurrencyId = currencies[0].Id,
						PaymentTermDays = 15
					}
				};
				context.Suppliers.AddRange(suppliers);
				context.SaveChanges();
				DateTime today = DateTime.Today;
				context.CurrencyConversions.Add(new CurrencyConversion
				{
					TenantId = tenantId,
					FromCurrencyId = currencies[1].Id,
					ToCurrencyId = currencies[0].Id,
					Rate = 122.5m,
					EffectiveDate = today.AddDays(-14.0)
				});
				context.UnitConversions.AddRange(new UnitConversion[3]
				{
					new UnitConversion
					{
						TenantId = tenantId,
						UnitSetId = sets[0].Id,
						FromUnitId = units[1].Id,
						ToUnitId = units[0].Id,
						Factor = 12m
					},
					new UnitConversion
					{
						TenantId = tenantId,
						UnitSetId = sets[1].Id,
						FromUnitId = units[2].Id,
						ToUnitId = units[3].Id,
						Factor = 1000m
					},
					new UnitConversion
					{
						TenantId = tenantId,
						UnitSetId = sets[1].Id,
						FromUnitId = units[3].Id,
						ToUnitId = units[2].Id,
						Factor = 0.001m
					}
				});
				var specs = new[]
				{
					new
					{
						Code = "ITM-001",
						Name = "Smartphone X1",
						Category = 0,
						Sub = 0,
						Unit = 0,
						Cost = 18000m,
						Sale = 22000m,
						Reorder = 5m
					},
					new
					{
						Code = "ITM-002",
						Name = "Wireless Earbuds",
						Category = 0,
						Sub = 1,
						Unit = 0,
						Cost = 1800m,
						Sale = 2900m,
						Reorder = 15m
					},
					new
					{
						Code = "ITM-003",
						Name = "USB-C Cable",
						Category = 0,
						Sub = 1,
						Unit = 0,
						Cost = 120m,
						Sale = 250m,
						Reorder = 50m
					},
					new
					{
						Code = "ITM-004",
						Name = "Power Bank 10000mAh",
						Category = 0,
						Sub = 1,
						Unit = 0,
						Cost = 1500m,
						Sale = 2200m,
						Reorder = 10m
					},
					new
					{
						Code = "ITM-005",
						Name = "Green Tea",
						Category = 1,
						Sub = 2,
						Unit = 2,
						Cost = 600m,
						Sale = 900m,
						Reorder = 10m
					},
					new
					{
						Code = "ITM-006",
						Name = "Black Coffee",
						Category = 1,
						Sub = 2,
						Unit = 2,
						Cost = 850m,
						Sale = 1300m,
						Reorder = 8m
					}
				};
				string[] emoji = new string[6] { "\ud83d\udcf1", "\ud83c\udfa7", "\ud83d\udd0c", "\ud83d\udd0b", "\ud83c\udf75", "" };
				string[] colors = new string[6] { "#dbeafe", "#ede9fe", "#fef3c7", "#dcfce7", "#d1fae5", "#ffffff" };
				ItemMaster[] items = specs.Select((s, index) => new ItemMaster
				{
					TenantId = tenantId,
					ItemCode = s.Code,
					ItemName = s.Name,
					ItemCategoryId = categories[s.Category].Id,
					ItemSubCategoryId = subs[s.Sub].Id,
					UnitSetId = units[s.Unit].UnitSetId,
					BaseUnitId = units[s.Unit].Id,
					StandardCost = s.Cost,
					SalePrice = s.Sale,
					ReorderLevel = s.Reorder,
					PictureId = (string.IsNullOrEmpty(emoji[index]) ? ((Guid?)null) : new Guid?(AddTile(context, tenantId, emoji[index], colors[index])))
				}).ToArray();
				context.ItemMasters.AddRange(items);
				context.SaveChanges();
				decimal[] onHand = new decimal[6] { 20m, 40m, 300m, 25m, 20m, 15m };
				decimal[] average = new decimal[6] { 18055.56m, 1800m, 119.33m, 1491.67m, 600m, 853.33m };
				decimal[] last = new decimal[6] { 18000m, 1800m, 118m, 1480m, 600m, 860m };
				for (int i = 0; i < items.Length; i++)
				{
					context.StockBalances.Add(new StockBalance
					{
						TenantId = tenantId,
						ItemMasterId = items[i].Id,
						QuantityOnHand = onHand[i],
						AverageCost = average[i],
						LastPurchaseCost = last[i],
						LastMovementTime = today.AddDays(-1.0)
					});
					context.StockMovements.Add(new StockMovement
					{
						TenantId = tenantId,
						ItemMasterId = items[i].Id,
						MovementDate = today.AddDays(-14.0),
						MovementType = StockMovementType.Opening,
						Quantity = (new decimal[6] { 20m, 40m, 300m, 25m, 20m, 15m })[i],
						UnitCost = specs[i].Cost,
						TotalValue = (new decimal[6] { 20m, 40m, 300m, 25m, 20m, 15m })[i] * specs[i].Cost,
						BalanceAfter = (new decimal[6] { 20m, 40m, 300m, 25m, 20m, 15m })[i],
						ReferenceType = "Opening",
						ReferenceNo = "Opening"
					});
				}
				var activity = new[]
				{
					new
					{
						Day = -13,
						Item = 3,
						Type = StockMovementType.SaleOut,
						Qty = -5m,
						Cost = 1500m,
						Ref = "IS-206"
					},
					new
					{
						Day = -12,
						Item = 1,
						Type = StockMovementType.SaleOut,
						Qty = -10m,
						Cost = 1800m,
						Ref = "IS-207"
					},
					new
					{
						Day = -12,
						Item = 5,
						Type = StockMovementType.SaleOut,
						Qty = -3m,
						Cost = 850m,
						Ref = "IS-208"
					},
					new
					{
						Day = -11,
						Item = 0,
						Type = StockMovementType.SaleOut,
						Qty = -3m,
						Cost = 18000m,
						Ref = "IS-209"
					},
					new
					{
						Day = -10,
						Item = 2,
						Type = StockMovementType.SaleOut,
						Qty = -60m,
						Cost = 120m,
						Ref = "IS-210"
					},
					new
					{
						Day = -10,
						Item = 4,
						Type = StockMovementType.SaleOut,
						Qty = -4m,
						Cost = 600m,
						Ref = "IS-211"
					},
					new
					{
						Day = -9,
						Item = 1,
						Type = StockMovementType.SaleOut,
						Qty = -12m,
						Cost = 1800m,
						Ref = "IS-212"
					},
					new
					{
						Day = -9,
						Item = 3,
						Type = StockMovementType.SaleOut,
						Qty = -6m,
						Cost = 1500m,
						Ref = "IS-213"
					},
					new
					{
						Day = -8,
						Item = 0,
						Type = StockMovementType.SaleOut,
						Qty = -4m,
						Cost = 18000m,
						Ref = "IS-214"
					},
					new
					{
						Day = -7,
						Item = 2,
						Type = StockMovementType.PurchaseIn,
						Qty = 120m,
						Cost = 118m,
						Ref = "PI-215"
					},
					new
					{
						Day = -6,
						Item = 1,
						Type = StockMovementType.SaleOut,
						Qty = -6m,
						Cost = 1800m,
						Ref = "IS-216"
					},
					new
					{
						Day = -6,
						Item = 4,
						Type = StockMovementType.SaleOut,
						Qty = -5m,
						Cost = 600m,
						Ref = "IS-217"
					},
					new
					{
						Day = -5,
						Item = 0,
						Type = StockMovementType.PurchaseIn,
						Qty = 5m,
						Cost = 18200m,
						Ref = "PI-218"
					},
					new
					{
						Day = -5,
						Item = 5,
						Type = StockMovementType.SaleOut,
						Qty = -2m,
						Cost = 850m,
						Ref = "IS-219"
					},
					new
					{
						Day = -4,
						Item = 2,
						Type = StockMovementType.SaleOut,
						Qty = -90m,
						Cost = 119.33m,
						Ref = "IS-220"
					},
					new
					{
						Day = -4,
						Item = 3,
						Type = StockMovementType.PurchaseIn,
						Qty = 10m,
						Cost = 1480m,
						Ref = "PI-0100"
					},
					new
					{
						Day = -3,
						Item = 0,
						Type = StockMovementType.SaleOut,
						Qty = -6m,
						Cost = 18055.56m,
						Ref = "IS-222"
					},
					new
					{
						Day = -3,
						Item = 4,
						Type = StockMovementType.SaleOut,
						Qty = -3m,
						Cost = 600m,
						Ref = "IS-223"
					},
					new
					{
						Day = -2,
						Item = 1,
						Type = StockMovementType.SaleOut,
						Qty = -5m,
						Cost = 1800m,
						Ref = "IS-224"
					},
					new
					{
						Day = -2,
						Item = 2,
						Type = StockMovementType.SaleOut,
						Qty = -40m,
						Cost = 119.33m,
						Ref = "IS-225"
					},
					new
					{
						Day = -1,
						Item = 0,
						Type = StockMovementType.SaleOut,
						Qty = -4m,
						Cost = 18055.56m,
						Ref = "IS-226"
					},
					new
					{
						Day = -1,
						Item = 3,
						Type = StockMovementType.SaleOut,
						Qty = -3m,
						Cost = 1491.67m,
						Ref = "IS-227"
					},
					new
					{
						Day = -1,
						Item = 5,
						Type = StockMovementType.PurchaseIn,
						Qty = 5m,
						Cost = 860m,
						Ref = "PI-228"
					}
				};
				decimal[] balances = new decimal[6] { 20m, 40m, 300m, 25m, 20m, 15m };
				var array = activity;
				foreach (var a in array)
				{
					balances[a.Item] += a.Qty;
					context.StockMovements.Add(new StockMovement
					{
						TenantId = tenantId,
						ItemMasterId = items[a.Item].Id,
						MovementDate = today.AddDays(a.Day),
						MovementType = a.Type,
						Quantity = a.Qty,
						UnitCost = a.Cost,
						TotalValue = Math.Abs(a.Qty * a.Cost),
						BalanceAfter = balances[a.Item],
						ReferenceType = ((a.Type == StockMovementType.PurchaseIn) ? "PurchaseInvoice" : "IssueSale"),
						ReferenceNo = a.Ref
					});
				}
				PurchaseInvoice purchase = new PurchaseInvoice
				{
					TenantId = tenantId,
					InvoiceNo = "PI-0100",
					InvoiceDate = today.AddDays(-4.0),
					SupplierId = suppliers[0].Id,
					CurrencyId = currencies[0].Id,
					ExchangeRate = 1m,
					Status = DocumentStatus.Posted,
					IsPosted = true,
					PostedTime = today.AddDays(-4.0),
					TotalAmount = 14800m,
					SubTotal = 14800m
				};
				purchase.Items.Add(new PurchaseInvoiceItem
				{
					TenantId = tenantId,
					ItemMasterId = items[3].Id,
					UnitMasterId = units[0].Id,
					Quantity = 10m,
					ConversionFactor = 1m,
					BaseQuantity = 10m,
					UnitCost = 1480m,
					LineTotal = 14800m
				});
				PurchaseInvoice draftPurchase = new PurchaseInvoice
				{
					TenantId = tenantId,
					InvoiceNo = "PI-0101",
					InvoiceDate = today,
					SupplierId = suppliers[0].Id,
					CurrencyId = currencies[0].Id,
					ExchangeRate = 1m,
					Status = DocumentStatus.Draft,
					TotalAmount = 54000m,
					SubTotal = 54000m
				};
				draftPurchase.Items.Add(new PurchaseInvoiceItem
				{
					TenantId = tenantId,
					ItemMasterId = items[1].Id,
					UnitMasterId = units[0].Id,
					Quantity = 30m,
					ConversionFactor = 1m,
					BaseQuantity = 30m,
					UnitCost = 1800m,
					LineTotal = 54000m
				});
				IssueSale draftSale = new IssueSale
				{
					TenantId = tenantId,
					DocumentNo = "IS-0001",
					DocumentDate = today,
					CustomerName = "Walk-in customer",
					IssueType = IssueSaleType.Sale,
					CurrencyId = currencies[0].Id,
					ExchangeRate = 1m,
					Status = DocumentStatus.Draft,
					TotalAmount = 44000m,
					SubTotal = 44000m
				};
				draftSale.Items.Add(new IssueSalesItem
				{
					TenantId = tenantId,
					ItemMasterId = items[0].Id,
					UnitMasterId = units[0].Id,
					Quantity = 2m,
					ConversionFactor = 1m,
					BaseQuantity = 2m,
					UnitPrice = 22000m,
					UnitCost = average[0],
					LineTotal = 44000m
				});
				context.PurchaseInvoices.Add(purchase);
				context.PurchaseInvoices.Add(draftPurchase);
				context.IssueSales.Add(draftSale);
				context.SaveChanges();
			}
		}

		private static Guid AddTile(StockPilotDbContext context, int tenantId, string label, string color)
		{
			using (Bitmap bitmap = new Bitmap(64, 64))
			{
				using (Graphics graphics = Graphics.FromImage(bitmap))
				{
					using (MemoryStream stream = new MemoryStream())
					{
						graphics.Clear(ColorTranslator.FromHtml(color));
						using (Font font = new Font("Segoe UI Emoji", 32f, FontStyle.Regular, GraphicsUnit.Pixel))
						{
							using (SolidBrush brush = new SolidBrush(Color.FromArgb(35, 45, 65)))
							{
								using (StringFormat format = new StringFormat
								{
									Alignment = StringAlignment.Center,
									LineAlignment = StringAlignment.Center
								})
								{
									graphics.DrawString(label, font, brush, new RectangleF(0f, 0f, 64f, 64f), format);
								}
							}
						}
						bitmap.Save(stream, ImageFormat.Jpeg);
						BinaryObject binary = new BinaryObject(tenantId, stream.ToArray());
						context.BinaryObjects.Add(binary);
						return binary.Id;
					}
				}
			}
		}
	}
}
