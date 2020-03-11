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

		// TODO Pass Addresses as method params after they have been validated by UPS API
		public static TransactionModel CreateSalesOrderTransaction(IInvoice i, ICustomerBase customer)
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
				string msg = "Invoice is null. Cannot create Transaction Builder ";
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

			// CustomerCode currently determined according to registered/anonymous status (invoice Key or email)
			string customerCode = string.Empty;
			if(customer != null && !customer.Key.Equals(Guid.Empty))
			{
				customerCode = customer.Key.ToString();
			} else
			{
				// Anonymous customers or other issues retrieving a related customer
				customerCode = HttpUtility.UrlEncode(i.BillToEmail);
			}

			// Create TB with base info			
			var tb = new TransactionBuilder(client, AvaTaxConstants.COMPANY_CODE, DocumentType.SalesOrder, customerCode);

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

				return null;
			}

			IEnumerable<IAddress> shipTo = null;
			try
			{
				shipTo = i.GetShippingAddresses();
			}
			catch (Exception ex)
			{
				LogHelper.Error(typeof(AvaTaxApiHelper), "Error getting ShipTo Address(es) during AvaTax Create Sales Order for Invoice " + i.Key + " ", ex);

				return null;
			}

			// Set ShipFrom Address (v.1.0 All Orders have a single origination address)
			tb.WithAddress(TransactionAddressType.ShipFrom, warehouse.Address1, warehouse.Address2, null, warehouse.Locality, warehouse.Region, warehouse.PostalCode, warehouse.CountryCode);

			// Set ShipTo Address (v.1.0 Though Orders can have multiple SHIPMENTS, they all share the same destination Address
			foreach (IAddress ship in shipTo)
			{
				tb.WithAddress(TransactionAddressType.ShipTo, ship.Address1, ship.Address2, null, ship.Locality, ship.Region, ship.PostalCode, ship.CountryCode);
			}

			#endregion Addresses

			#region Products / Line Items

			IEnumerable<ILineItem> lineItems = i.ProductLineItems();
			if (lineItems != null && lineItems.Any())
			{
				foreach (ILineItem li in lineItems)
				{					
					if (li.ExtendedData.GetTaxableValue())
					{
						string avaProductTaxCode = GetAvaTaxCodeForLineItem(li);
						if (string.IsNullOrEmpty(avaProductTaxCode))
						{
							// Error, no product tax code
							Exception ex = new Exception("avaProductTaxCode is null for line item " + li.Key + " on Invoice " + i.Key);
							LogHelper.Error(typeof(AvaTaxApiHelper), "Could not determine product tax code for AvaTax. ", ex);
							continue;
						}

						Guid productVariantKey = li.ExtendedData.GetProductVariantKey();
						// TODO Fallback to parent product key?

						tb.WithLine(li.Price, li.Quantity, avaProductTaxCode, "description", productVariantKey.ToString(), "TODO_customerUsageType", li.Key.ToString());
					}
					else
					{
						tb.WithExemptLine(li.Price, "TODO_ExemptionCode");
					}
				}
			}

			#endregion Products / Line Items

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

			TransactionModel tm = null;

			if (tb != null)
			{
				try
				{
					tm = tb.Create();

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

		public static TransactionModel CreateSalesInvoiceTransaction(IInvoice i)
		{
			bool commit = false; // Website will not have authority to commit any transactions in v.1.0

			TransactionModel tm = null;

			// Use TransactionModel as is and set to Tax Line Item collection...

			return tm;
		}

		public static string GetAvaTaxCodeForLineItem(ILineItem lineItem)
		{
			string taxCode = string.Empty;

			var pvKey = lineItem.ExtendedData.GetProductVariantKey();
			IProductVariant pv = null;
			if (pvKey != null)
			{
				pv = MerchelloContext.Current.Services.ProductVariantService.GetByKey(pvKey);
				// MH isn't available in Merchello.Core
				//MerchelloHelper mh = new MerchelloHelper();
				//product = mh.TypedProductContent(productKey);
			}
			if (pv == null)
			{
				Exception ex = new Exception("No Product Variant found with Key " + pvKey + " in lineItem " + lineItem.Key.ToString());
				LogHelper.Error(typeof(AvaTaxLineItemVisitor), "Error retrieving Product Variant for lineItem. ", ex);
				return string.Empty;
			}
			//pv.Master // bool, is master variant
			//pv.DetachedContents.First().DetachedContentType
			if (pv.DetachedContents != null && pv.DetachedContents.Any())
			{
				// TODO v.1.5 Pass this DocType value from front-end project instead of having the Tax provider inherintely know about it
				taxCode = pv.DetachedContents.First().DetachedDataValues.FirstOrDefault(x => x.Key == "avaTaxProductCode").Value; // TODO AvaTaxConstants.AvaTaxProductCodeDocTypePropertyAlias
			}

			return taxCode;
		}

	}
}
