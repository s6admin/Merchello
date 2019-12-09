using Avalara.AvaTax.RestClient;
using Merchello.Core.Events;
using Merchello.Core.Gateways.Taxation.AvaTax.Constants;
using Merchello.Core.Models;
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
			ITaxationContext taxContext = MerchelloContext.Current.Gateways.Taxation;
			
			// S6 TODO This is a custom ED collection...where is it ultimately stored? Or is it temporary and then disposed? Invoices don't have ED collections, but their lineItems do
			var extendedData = new ExtendedDataCollection();
			
			if (Invoice.Key.Equals(Guid.Empty))
			{
				// DEPRECATED TODO Remove if temporary line item keys end up being sufficient
				// Set a flag indicating the tax result has not yet been processed by the external provider so website project can respond if needed
				// This becomes the invoice TaxLineItem ED collection
				extendedData.SetValue(AvaTaxConstants.AWAITING_AVATAX_KEY, bool.TrueString);

				//	// Success must be sent when the invoice is intially saved otherwise eComm will error
				//	// TODO Consider passing an empty result if the taxMethod Name and extendedData collection are not required. That will make identifying empty results from valid 0% tax results easier
				//return Attempt<ITaxCalculationResult>.Succeed(
			    //	new TaxCalculationResult(_taxMethod.Name, 0, 0, extendedData)); // new TaxCalculationResult(0,0)); 

			}  	
					
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
					TransactionModel tm = null;
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
						LogHelper.Error(typeof(AvaTaxCalculationStrategy), "AvaTax Create Sales Order failed. ", ex);

						return Attempt<ITaxCalculationResult>.Fail(ex);
					}

					// Fire SalesOrder Success event so front-end can handle any custom tasks						
					OnSalesOrderSuccess.RaiseEvent(new ObjectEventArgs<TransactionModel>(tm), this);

					// S6 TODO Is baseTaxRate required by ALL Tax providers or just FixedRate, which is being phased out?
					// Rate is returned from AvaTax in the summary collection and can have multiple values. Determine how to represent this (if necessary) within the ED collection
					var baseTaxRate = 0; // tm.summary.First().rate ?? 0;
					extendedData.SetValue(Core.Constants.ExtendedDataKeys.BaseTaxRate, baseTaxRate.ToString(CultureInfo.InvariantCulture));
										
					extendedData.SetValue(AvaTaxConstants.SALES_ORDER_KEY, tm); // TODO Track where this ED collection ultimately ends up
						
					var visitor = new AvaTaxLineItemVisitor(tm);

					decimal totalTax = tm.totalTax ?? 0;

					// Flag if totalTax is NULL, it may need to be treated differently than 0, currently permitted to continue processing
					if (tm.totalTax == null) {
						
						Exception ex = new Exception("totalTax is NULL for transaction " + tm.id + " (" + tm.email + ") ");
						LogHelper.Error(typeof(AvaTaxCalculationStrategy), "Warning: AvaTax transaction returned with NULL totalTax. ", ex);
					}				
												
					return Attempt<ITaxCalculationResult>.Succeed(
						new TaxCalculationResult(_taxMethod.Name, baseTaxRate, totalTax, extendedData));
					
				}
				catch (Exception ex)
				{
					LogHelper.Error(typeof(AvaTaxCalculationStrategy), "Error creating AvaTax SalesOrder Transaction. ", ex);

					return Attempt<ITaxCalculationResult>.Fail(ex);
				}				
			} else
			{
				Exception ex = new Exception("AvaTax service connection failed. Initialization method returned 'false'. ");

				return Attempt<ITaxCalculationResult>.Fail(ex);
			}
			

			// S6 From FlatRate, for reference only
			//try
			//{
			//	var baseTaxRate = _taxMethod.PercentageTaxRate;

			//	// S6 TODO Is BaseTaxRate going to be the AvaTax Sales Order (estimate) result?
			//	extendedData.SetValue(Core.Constants.ExtendedDataKeys.BaseTaxRate, baseTaxRate.ToString(CultureInfo.InvariantCulture));

			//	if (_taxMethod.HasProvinces)
			//	{
			//		baseTaxRate = AdjustedRate(baseTaxRate, _taxMethod.Provinces.FirstOrDefault(x => x.Code == TaxAddress.Region), extendedData);
			//	}

			//	// S6 Visitor applies to each line item. AvaTax response is for the entire basket so we may need to distribute the result to each line Item
			//	var visitor = new TaxableLineItemVisitor(baseTaxRate / 100);

			//	Invoice.Items.Accept(visitor);
				
			//	var totalTax = visitor.TaxableLineItems.Sum(x => decimal.Parse(x.ExtendedData.GetValue(Core.Constants.ExtendedDataKeys.LineItemTaxAmount), CultureInfo.InvariantCulture));
				
			//	return Attempt<ITaxCalculationResult>.Succeed(
			//		new TaxCalculationResult(_taxMethod.Name, baseTaxRate, totalTax, extendedData));
			//}
			//catch (Exception ex)
			//{
			//	return Attempt<ITaxCalculationResult>.Fail(ex);
			//}
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
