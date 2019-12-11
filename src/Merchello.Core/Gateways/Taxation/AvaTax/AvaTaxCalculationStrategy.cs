using Avalara.AvaTax.RestClient;
using Merchello.Core.Events;
using Merchello.Core.Gateways.Taxation.AvaTax.Constants;
using Merchello.Core.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Events;
using Umbraco.Core.Logging;

namespace Merchello.Core.Gateways.Taxation.AvaTax
{
	// S6 Changed from internal to public so events can be fired for front-end project. TODO Determine if this exposes too much
	public class AvaTaxCalculationStrategy : TaxCalculationStrategyBase
	{
		private readonly ITaxMethod _taxMethod;

		public static event TypedEventHandler<AvaTaxCalculationStrategy, Merchello.Core.Events.ObjectEventArgs<TransactionModel>> OnSalesOrderSuccess;
		public static event TypedEventHandler<AvaTaxCalculationStrategy, Merchello.Core.Events.ObjectEventArgs<TransactionModel>> OnSalesOrderFail;
		public static event TypedEventHandler<AvaTaxCalculationStrategy, Merchello.Core.Events.ObjectEventArgs<TransactionModel>> OnSalesInvoiceSuccess;
		public static event TypedEventHandler<AvaTaxCalculationStrategy, Merchello.Core.Events.ObjectEventArgs<TransactionModel>> OnSalesInvoiceFail;

		public AvaTaxCalculationStrategy(IInvoice invoice, IAddress taxAddress, ITaxMethod taxMethod)
			: base(invoice, taxAddress)
		{
			Mandate.ParameterNotNull(taxMethod, "countryTaxRate");
			_taxMethod = taxMethod;
		}

		public override Attempt<ITaxCalculationResult> CalculateTaxesForInvoice(bool quoteOnly = false)
		{
			ITaxationContext taxContext = MerchelloContext.Current.Gateways.Taxation;

			if (string.IsNullOrEmpty(taxContext.TaxationProviderUsername))
			{
				Exception ex = new Exception("AvaTax Provider requires authentication. Missing Username. ");
				LogHelper.Error(typeof(AvaTaxCalculationStrategy), "Error complete tax calculation request. ", ex);

				// TODO Fail or return empty success?
				return Attempt<ITaxCalculationResult>.Succeed(
					new TaxCalculationResult(_taxMethod.Name, 0, 0));
			}

			if (string.IsNullOrEmpty(taxContext.TaxationProviderPassword))
			{
				Exception ex = new Exception("AvaTax Provider requires authentication. Missing Password. ");
				LogHelper.Error(typeof(AvaTaxCalculationStrategy), "Error complete tax calculation request. ", ex);

				// TODO Fail or return empty success?
				return Attempt<ITaxCalculationResult>.Succeed(
					new TaxCalculationResult(_taxMethod.Name, 0, 0));
			}

			// TODO If this method hits, revert any eComm core classe changes that introduced s6 user/pswd as method parameters 
			return CalculateTaxesForInvoice(taxContext.TaxationProviderUsername, taxContext.TaxationProviderPassword, quoteOnly);	
		}

		public override Attempt<ITaxCalculationResult> CalculateTaxesForInvoice(string user, string pswd, bool quoteOnly = false)
		{

			string transactionTypeLabel = quoteOnly ? "SalesOrder" : "SalesInvoice";
			IEnumerable<ILineItem> taxLines = null;
			ILineItem taxLine = null;

			// NOTE: This becomes the tax lineItem ED collection
			ExtendedDataCollection extendedData = null;

			// If this is a final tax call to SalesInvoice attempt to retrieve the ED from the existing invoice tax line item so new values can be added to it, otherwise create an empty ED
			if (!quoteOnly)
			{
				// TODO CONSIDER IF AVATAX ORDERS SHOULD RESULT IN MULTIPLE TAX LINE ITEMS OR JUST SPLIT THE VALUES BETWEEN THE PRODUCT LINE ITEMS...discuss with client as well
				taxLines = Invoice.TaxLineItems();
				if (taxLines != null && taxLines.Any())
				{
					taxLine = taxLines.First(); // TODO Current orders only have one tax line item, but that may change with AvaTax implementation
					if(taxLine != null && taxLine.ExtendedData != null)
					{
						extendedData = taxLine.ExtendedData;
					}
				}
			} 

			if(extendedData == null)
			{
				extendedData = new ExtendedDataCollection();
			}			
						
			if (Invoice.Key.Equals(Guid.Empty))
			{
				// DEPRECATED TODO Remove if temporary line item keys end up being sufficient
				// Set a flag indicating the tax result has not yet been processed by the external provider so website project can respond if needed
				// This becomes the invoice TaxLineItem ED collection
				//extendedData.SetValue(AvaTaxConstants.AWAITING_AVATAX_KEY, bool.TrueString);

				//	// Success must be sent when the invoice is intially saved otherwise eComm will error
				//	// TODO Consider passing an empty result if the taxMethod Name and extendedData collection are not required. That will make identifying empty results from valid 0% tax results easier
				//return Attempt<ITaxCalculationResult>.Succeed(
			    //	new TaxCalculationResult(_taxMethod.Name, 0, 0, extendedData)); // new TaxCalculationResult(0,0)); 

			}

			TransactionModel tm = null;

			if (AvaTaxApiHelper.Init(user, pswd))
			{
				try
				{
					ICustomerBase c = Invoice.Customer(); // Passing Merchello.Context returned null customer
					if(c == null)
					{
						//MerchelloContext.Current.Services.CustomerService
					}

					// Value of quoteOnly determines whether CreateSalesOrder or CreateInvoice is called					
					if (quoteOnly)
					{
						tm = AvaTaxApiHelper.CreateSalesOrderTransaction(Invoice, c);
					} else
					{
						tm = AvaTaxApiHelper.CreateSalesInvoiceTransaction(Invoice, c);
					}
										
					if (tm == null)
					{
						Exception ex = new Exception("NULL Transaction Model returned for Invoice " + Invoice.Key.ToString());
						LogHelper.Error(typeof(AvaTaxCalculationStrategy), "AvaTax " + transactionTypeLabel + " failed. ", ex);

						BroadcastFail(tm, quoteOnly);

						return Attempt<ITaxCalculationResult>.Fail(ex);
					}
										
					// S6 TODO Is baseTaxRate required by ALL Tax providers or just FixedRate, which is being phased out?
					// Rate is returned from AvaTax in the summary collection and can have multiple values. Determine how to represent this (if necessary) within the ED collection
					var baseTaxRate = 0; // tm.summary.First().rate ?? 0;
					extendedData.SetValue(Core.Constants.ExtendedDataKeys.BaseTaxRate, baseTaxRate.ToString(CultureInfo.InvariantCulture));

					// Only include properties with actual values as part of the stored Json data
					string tmData = string.Empty;

					try
					{
						tmData = JsonConvert.SerializeObject(tm, Formatting.None,
							   new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }
					   		);
					} catch(Exception ex)
					{
						LogHelper.Error(typeof(AvaTaxCalculationStrategy), "Error parsing AvaTax " + transactionTypeLabel + " Json. ", ex);
					}

					if (quoteOnly)
					{
						extendedData.SetValue(AvaTaxConstants.SALES_ORDER_KEY, tmData); 
					} else
					{
						// Save to existing tax line item ED if available
						if(taxLine != null)
						{
							taxLine.ExtendedData.SetValue(AvaTaxConstants.SALES_INVOICE_KEY, tmData);
						} else
						{
							// The SalesInvoice transaction has still likely succeeded remotely so permit execution to continue	after logging issue						
							Exception ex = new Exception("Could not locate tax line item for existing promise invoice " + Invoice.Key);
							LogHelper.Error(typeof(AvaTaxCalculationStrategy), "Could not save SalesInvoice data to promise Invoice. ", ex);							
							
						}
						// TODO Just save the SalesInvoice transaction Id since the data can be retrieved from the AvaTax API, unlike SalesOrders which are not recorded in their system
						// extendedData.SetValue(AvaTaxConstants.SALES_INVOICE_KEY, tm.id.ToString());						
					}					
						
					var visitor = new AvaTaxLineItemVisitor(tm);

					Invoice.Items.Accept(visitor);

					decimal totalTax = tm.totalTax ?? 0;

					// Flag if totalTax is NULL, it may need to be treated differently than 0, currently permitted to continue processing
					if (tm.totalTax == null) {
						
						Exception ex = new Exception("totalTax is NULL for transaction " + tm.id + " (" + tm.email + ") ");
						LogHelper.Error(typeof(AvaTaxCalculationStrategy), "Warning: AvaTax " + transactionTypeLabel + " transaction returned with NULL totalTax. ", ex);
					}

					// If tax calculation is final (SalesInvoice), ensure the Invoice is saved
					if (!quoteOnly)
					{
						MerchelloContext.Current.Services.InvoiceService.Save(Invoice, false); // TODO Consider if events are helpful here or not
					}

					BroadcastSuccess(tm, quoteOnly);

					return Attempt<ITaxCalculationResult>.Succeed(
						new TaxCalculationResult(_taxMethod.Name, baseTaxRate, totalTax, extendedData));
					
				}
				catch (Exception ex)
				{
					LogHelper.Error(typeof(AvaTaxCalculationStrategy), "Error creating AvaTax " + transactionTypeLabel + " Transaction. ", ex);

					BroadcastFail(tm, quoteOnly);

					return Attempt<ITaxCalculationResult>.Fail(ex);
				}				
			} else
			{
				Exception ex = new Exception("AvaTax service connection failed. Initialization method returned 'false'. ");

				BroadcastFail(tm, quoteOnly);

				return Attempt<ITaxCalculationResult>.Fail(ex);
			}			
		}

		/// <summary>
		/// Fire appropriate Success event for any external project handlers
		/// </summary>
		/// <param name="tm">The tm.</param>
		/// <param name="quoteOnly">if set to <c>true</c> [quote only].</param>
		private void BroadcastSuccess(TransactionModel tm, bool quoteOnly = true)
		{			
			if (quoteOnly)
			{
				OnSalesOrderSuccess.RaiseEvent(new ObjectEventArgs<TransactionModel>(tm), this);
			}
			else
			{
				OnSalesInvoiceSuccess.RaiseEvent(new ObjectEventArgs<TransactionModel>(tm), this);
			}
		}

		/// <summary>
		/// Fire appropriate Fail event for any external project handlers
		/// </summary>
		/// <param name="tm">The tm.</param>
		/// <param name="quoteOnly">if set to <c>true</c> [quote only].</param>
		private void BroadcastFail(TransactionModel tm, bool quoteOnly = true)
		{
			if (quoteOnly)
			{
				OnSalesOrderFail.RaiseEvent(new ObjectEventArgs<TransactionModel>(tm), this);
			}
			else
			{
				OnSalesInvoiceFail.RaiseEvent(new ObjectEventArgs<TransactionModel>(tm), this);
			}
		}

		/// <summary>
		/// Finalizes taxes for the completed invoice via the AvaTax API without marking the SalesInvocie as committed.
		/// </summary>
		/// <param name="invoice">The invoice.</param>
		//public void FinalizeTaxesForCompletedInvoice(IInvoice invoice)
		//{
		//	AvaTaxApiHelper.CreateSalesInvoiceTransaction(invoice);
		//}

		// S6 TODO Finish (copied from FixedRate)
		private static decimal AdjustedRate(decimal baseRate, ITaxProvince province, ExtendedDataCollection extendedData)
		{
			if (province == null) return baseRate;
			extendedData.SetValue(Core.Constants.ExtendedDataKeys.ProviceTaxRate, province.PercentAdjustment.ToString(CultureInfo.InvariantCulture));
			return province.PercentAdjustment + baseRate;
		}
		
	}
}
