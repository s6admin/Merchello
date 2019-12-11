using Avalara;
using Avalara.AvaTax;
using Avalara.AvaTax.RestClient;
using Merchello.Core;
using Merchello.Core.Gateways.Taxation.AvaTax.Constants;
using Merchello.Core.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Web;
using Umbraco.Core.Logging;
using Umbraco.Web;

namespace Merchello.Core.Gateways.Taxation.AvaTax
{
	internal class AvaTaxApiHelper
	{
		private static AvaTaxClient client = null;

		public static bool Init(string user, string pswd)
		{
			if(client == null)
			{				
				try
				{
					// TODO AppName and version unless they are just for decoration
					client = new AvaTaxClient("MyTestAppName", "1.0", Environment.MachineName, AvaTaxEnvironment.Sandbox)
					.WithSecurity(user, pswd);
				}
				catch (Exception ex)
				{
					LogHelper.Error(typeof(AvaTaxApiHelper), "Error initializing AvaTax Client. ", ex);
					return false;
				}
			}			

			if (client != null)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		private static string DetermineCustomerCode(IInvoice i, ICustomerBase customer = null)
		{			
			// CustomerCode currently determined according to registered/anonymous status (invoice Key or email)
			string customerCode = string.Empty;

			if(customer == null)
			{
				// Attempt retrieving customer from invoice
				customer = i.Customer();
			}

			if (customer != null && !customer.Key.Equals(Guid.Empty))
			{
				customerCode = customer.Key.ToString();
			}
			else
			{
				// Anonymous customers or other issues retrieving a related customer
				customerCode = HttpUtility.UrlEncode(i.BillToEmail);
			}

			return customerCode;
		}

		private static void SetAddresses(TransactionBuilder tb, IInvoice i)
		{
			#region Addresses

			// TODO Address logic may move out of AvaTaxHelper once UPS API address validation has been added
			IWarehouse warehouse = null;
			try
			{
				warehouse = MerchelloContext.Current.Services.WarehouseService.GetDefaultWarehouse();
			}
			catch (Exception ex)
			{
				LogHelper.Error(typeof(AvaTaxApiHelper), "Error getting ShipFrom Address during AvaTax Create Sales Order for Invoice " + i.Key + " ", ex);

				return;
			}

			IEnumerable<IAddress> shipTo = null;
			try
			{
				shipTo = i.GetShippingAddresses();
			}
			catch (Exception ex)
			{
				LogHelper.Error(typeof(AvaTaxApiHelper), "Error getting ShipTo Address(es) during AvaTax Create Sales Order for Invoice " + i.Key + " ", ex);

				return;
			}

			// Set ShipFrom Address (v.1.0 All Orders have a single origination address)
			tb.WithAddress(TransactionAddressType.ShipFrom, warehouse.Address1, warehouse.Address2, null, warehouse.Locality, warehouse.Region, warehouse.PostalCode, warehouse.CountryCode);

			// Set ShipTo Address (v.1.0 Though Orders can have multiple SHIPMENTS, they all share the same destination Address
			foreach (IAddress ship in shipTo)
			{
				// TODO No way to specify unique "id" of address? (returned in AvaTax response)
				tb.WithAddress(TransactionAddressType.ShipTo, ship.Address1, ship.Address2, null, ship.Locality, ship.Region, ship.PostalCode, ship.CountryCode);
			}

			#endregion Addresses
		}

		private static void SetProducts(TransactionBuilder tb, IInvoice i, bool quoteOnly = true)
		{
			#region Products / Line Items

			IEnumerable<ILineItem> lineItems = i.ProductLineItems();
			if (lineItems != null && lineItems.Any())
			{
				foreach (ILineItem li in lineItems)
				{
					if (li.ExtendedData.GetTaxableValue())
					{
						string avaProductTaxCode = GetAvaTaxCodeForLineItem(li);
						Guid productKey = GetProductKeyForLineItem(li); // Will return the appropriate variant or parent key depending on the specific product
						if (string.IsNullOrEmpty(avaProductTaxCode))
						{
							// Error, no product tax code
							Exception ex = new Exception("avaProductTaxCode is null for line item " + li.Key + " on Invoice " + i.Key);
							LogHelper.Error(typeof(AvaTaxApiHelper), "Could not determine product tax code for AvaTax. ", ex);
							continue;
						}

						Guid productVariantKey = li.ExtendedData.GetProductVariantKey(); // TODO Fallback to parent product key?

						Guid liAvaKey = GetLineItemAvaTaxKey(li, quoteOnly);

						if (liAvaKey.Equals(Guid.Empty))
						{
							Exception ex = new Exception("LineItem has no Key. ");
							LogHelper.Error(typeof(AvaTaxApiHelper), "Could not include lineItem in AvaTax calculation. ", ex);
							continue;
						}

						tb.WithLine(li.Price, li.Quantity, avaProductTaxCode, "TODO_description", productKey.ToString(), "TODO_customerUsageType", liAvaKey.ToString());
					}
					else
					{
						tb.WithExemptLine(li.Price, "TODO_ExemptionCode");
					}
				}
			}

			#endregion Products / Line Items
		}

		public static Guid GetLineItemAvaTaxKey(ILineItem li, bool quoteOnly = true)
		{
			Guid g = Guid.Empty;

			if(li != null)
			{
				g = li.Key; // Prioritize native eComm lineItem key
				
				// If native eComm lineItem is Empty but request if for a final tax record (SalesInvoice) there is an eComm issue. Allow temp lineItem Key fallback, but log the issue
				if(g.Equals(Guid.Empty) && !quoteOnly)
				{
					Exception ex = new Exception("No LineItem Key found for Product " + li.ExtendedData.GetProductKey() + " during AvaTax SalesInvoice request. Falling back to temporary lineItem Key used for SalesOrder estimate if available. ");
					LogHelper.Error(typeof(AvaTaxApiHelper), "No LineItem Key found during AvaTax SalesInvoice request. Falling back to temporary lineItem Key used for SalesOrder estimate. ", ex);
				}

				// If it is empty, and the request is only for a QUOTE (AvaTax SalesOrder) check for custom AvaTax key
				if (g.Equals(Guid.Empty) &&
					li.ExtendedData != null && 
					li.ExtendedData.ContainsKey(AvaTaxConstants.TEMPORARY_TAX_LINE_ITEM_KEY))
				{
					if(!Guid.TryParse(li.ExtendedData.GetValue(AvaTaxConstants.TEMPORARY_TAX_LINE_ITEM_KEY).ToString(), out g))
					{
						// Native LineItem key is empty AND LineItem has no temporary tax line item key
						Exception ex = new Exception("Could not determine Key for LineItem with product " + li.ExtendedData.GetProductKey());
						LogHelper.Error(typeof(AvaTaxApiHelper), "LineItem has no key. ", ex);
					} 
				}
			}

			return g;
		}

		// TODO Pass Addresses as method params after they have been validated by UPS API
		public static TransactionModel CreateSalesOrderTransaction(IInvoice i, ICustomerBase customer = null)
		{
			#region Ensure all required parameters are available
			
			// Ensure the AvaTax client is available
			if (client == null)
			{				
				Exception ex = new Exception("AvaTax Client is null. Cannot call tax provider for Invoice " + i.Key);
				LogHelper.Error(typeof(AvaTaxApiHelper), "Error during AvaTax Create Sales Order. ", ex);
				
				return null;				
			}

			if (i == null)
			{
				string msg = "Invoice is null. Cannot create Transaction Builder. ";
				if (customer != null)
				{
					msg += "for Customer " + customer.Key + ". ";
				}
				Exception ex = new Exception(msg);
				LogHelper.Error(typeof(AvaTaxApiHelper), "Error during AvaTax Create Sales Order. ", ex);

				return null;
			}

			// Customer can't be a requirement because anonymous invoices will always return null. Alternate could be to use Cart/Basket Key instead.
			/*if (customer == null)
			{
				Exception ex = new Exception("Customer is null. Cannot create Transation Builder for Invoice " + i.Key);
				LogHelper.Error(typeof(AvaTaxApiHelper), "Error during AvaTax Create Sales Order. ", ex);

				return null;
			}*/

			#endregion Ensure all required parameters are available

			string customerCode = DetermineCustomerCode(i, customer);
			
			// Create TB with base info			
			var tb = new TransactionBuilder(client, AvaTaxConstants.COMPANY_CODE, DocumentType.SalesOrder, customerCode);

			tb.WithPurchaseOrderNumber(i.Key.ToString());
			tb.WithDate(DateTime.Now);
			//.WithTransactionCode() // Unique transaction reference (required)
			//.WithType(DocumentType.SalesOrder) // Can be specified in construct params
			//.WithSeparateAddressLine(details)
			//.WithEmail("a@b.c")
			//.WithItemDiscount(bool)	
			//.WithDiscountAmount(decimal? discount)
			//.WithDate(DateTime.Now)
			//.WithExemptLine()
			//.WithExemptionNumber("exemptionNumber")

			SetAddresses(tb, i);

			SetProducts(tb, i);						

			TransactionModel tm = null;

			if (tb != null)
			{
				try
				{

					LogHelper.Warn(typeof(AvaTaxApiHelper), "AvaTax SalesOrder attempt for Invoice " + i.Key + " email " + i.BillToEmail);

					tm = tb.Create();

					LogHelper.Warn(typeof(AvaTaxApiHelper), "AvaTax SalesOrder success response " + tm.id + " for Invoice " + i.Key);

					return tm;
				}
				catch (AvaTaxError ex)
				{

					// TODO Handle errors https://developer.avalara.com/avatax/errors/
					// https://developer.avalara.com/avatax/common-errors/
					//Avalara.AvaTax.RestClient.ErrorTransactionOutputModel ae = new ErrorTransactionOutputModel();					
					
					LogHelper.Error(typeof(AvaTaxApiHelper), "Error during AvaTax CreateSalesOrder for Invoice " + i.Key + " " + ex.error, ex);
					
					return null;
				}
			}

			return null;
		}
				
		public static TransactionModel CreateSalesInvoiceTransaction(IInvoice i, ICustomerBase customer = null)
		{

			#region Ensure all required parameters are available

			// Ensure the AvaTax client is available
			if (client == null)
			{
				Exception ex = new Exception("AvaTax Client is null. Cannot call tax provider for Invoice " + i.Key);
				LogHelper.Error(typeof(AvaTaxApiHelper), "Error during AvaTax Create Sales Invoice. ", ex);

				return null;
			}

			if (i == null)
			{
				string msg = "Invoice is null. Cannot create Transaction Builder. ";
				if (customer != null)
				{
					msg += "for Customer " + customer.Key + ". ";
				}
				Exception ex = new Exception(msg);
				LogHelper.Error(typeof(AvaTaxApiHelper), "Error during AvaTax Create Sales Invoice. ", ex);

				return null;
			}

			#endregion Ensure all required parameters are available

			string customerCode = DetermineCustomerCode(i, customer);
			
			TransactionModel tm = null;
			var tb = new TransactionBuilder(client, AvaTaxConstants.COMPANY_CODE, DocumentType.SalesInvoice, customerCode);			
			tb.WithPurchaseOrderNumber(i.Key.ToString());
			tb.WithDate(DateTime.Now);
			
			SetAddresses(tb, i);

			SetProducts(tb, i);
			
			#region Call API

			if (tb != null)
			{
				try
				{
					LogHelper.Warn(typeof(AvaTaxApiHelper), "AvaTax SalesInvoice attempt for Invoice " + i.Key + " email " + i.BillToEmail);

					tm = tb.Create();
					
					LogHelper.Warn(typeof(AvaTaxApiHelper), "AvaTax SalesInvoice success response " + tm.id + " for Invoice " + i.Key);

					return tm;
				}
				catch (AvaTaxError ex)
				{

					// TODO Handle errors https://developer.avalara.com/avatax/errors/
					// https://developer.avalara.com/avatax/common-errors/					

					LogHelper.Error(typeof(AvaTaxApiHelper), "Error during AvaTax CreateSalesInvoice for eComm Invoice " + i.Key + " " + ex.error, ex);

					return null;
				}
			}

			#endregion Call API

			return null;
		}

		public static string GetAvaTaxCodeForLineItem(ILineItem lineItem)
		{
			string taxCode = string.Empty;
			
			// Options:
			//pv.Master // bool, is master variant
			//pv.DetachedContents.First().DetachedContentType

			// Check for productCode on variant first
			Guid pvKey = lineItem.ExtendedData.GetProductVariantKey();
			IProductVariant pv = null;
			if (!pvKey.Equals(Guid.Empty))
			{
				pv = MerchelloContext.Current.Services.ProductVariantService.GetByKey(pvKey);				
			}

			if (pv != null && !pv.Master) // If PV is master, just use base productCode (below)
			{
				Console.WriteLine(pv.Master);
				if (pv.DetachedContents != null && pv.DetachedContents.Any())
				{
					// TODO v.1.5 Pass this DocType value from front-end project instead of having the Tax provider inherently know how to retrieve it
					try
					{
						taxCode = pv.DetachedContents.First().DetachedDataValues.FirstOrDefault(x => x.Key == "avaTaxProductCode").Value; // TODO AvaTaxConstants.AvaTaxProductCodeDocTypePropertyAlias
					} catch(Exception ex)
					{
						LogHelper.Error(typeof(AvaTaxApiHelper), "Could not determine taxCode from Product Variant " + pvKey + " in lineItem " + lineItem.Key.ToString(), ex);
					}					
				}
			} else
			{
				// Else Fallback to parent product
				Guid pKey = lineItem.ExtendedData.GetProductKey();
				if (!pKey.Equals(Guid.Empty))
				{
					IProduct p = null;
					p = MerchelloContext.Current.Services.ProductService.GetByKey(pKey);

					if(p != null && p.DetachedContents != null && p.DetachedContents.Any())
					{						
						try
						{
							taxCode = p.DetachedContents.First().DetachedDataValues.FirstOrDefault(x => x.Key == "avaTaxProductCode").Value;
						} catch(Exception ex)
						{
							LogHelper.Error(typeof(AvaTaxApiHelper), "Could not determine taxCode from Product " + pKey + " in lineItem " + lineItem.Key.ToString(), ex);
						}						
					}
				}				
			}
			
			return taxCode;
		}

		private static Guid GetProductKeyForLineItem(ILineItem lineItem)
		{
			IProductVariant pv = null;
			Guid pvKey = lineItem.ExtendedData.GetProductVariantKey();
			if (!pvKey.Equals(Guid.Empty))
			{				
				pv = MerchelloContext.Current.Services.ProductVariantService.GetByKey(pvKey);
				if (pv.Master)
				{
					// If variant key is master, return core product key instead
					return lineItem.ExtendedData.GetProductKey();					
				}
			}

			// Return pv key if it is for an actual variant
			return pvKey;
		}
	}
}
