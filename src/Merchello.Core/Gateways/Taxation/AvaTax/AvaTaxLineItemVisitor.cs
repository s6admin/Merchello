using Avalara.AvaTax.RestClient;
using Merchello.Core.Gateways.Taxation.AvaTax.Constants;
using Merchello.Core.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Logging;

namespace Merchello.Core.Gateways.Taxation.AvaTax
{
	internal class AvaTaxLineItemVisitor : ILineItemVisitor
	{
		/// <summary>
		/// The line items identified as taxable.
		/// </summary>
		private readonly List<ILineItem> _lineItems = new List<ILineItem>();

		/// <summary>
		/// The tax rate to be applied to the line item.
		/// </summary>
		//private readonly decimal _taxRate;

		private readonly TransactionModel tm;

		/// <summary>
		/// Initializes a new instance of the <see cref="TaxableLineItemVisitor"/> class.
		/// </summary>
		public AvaTaxLineItemVisitor(TransactionModel transactionModel)
		{
			tm = transactionModel;
		}

		/// <summary>
		/// Gets the line items identified as taxable line items
		/// </summary>
		public IEnumerable<ILineItem> TaxableLineItems
		{
			get { return _lineItems; }
		}

		// S6 TODO Finished (copied from FixedRate)
		/// <summary>
		/// The visit.
		/// </summary>
		/// <param name="lineItem">
		/// The line item.
		/// </param>
		public void Visit(ILineItem lineItem)
		{
			// NOTE: Visitor must work without ANY invoice or lineItem keys, because none are generated during invoice preparation, only after an official invoice has been SAVED
			
			// If lineItem isn't marked taxable, skip it, otherwise continue below
			if (!lineItem.ExtendedData.GetTaxableValue()) return;
						
			// TODO PROBLEM: Without lineItem keys invoices with multiple lineItems of the same product won't be distinguishable from each other
			string avaItemCode = string.Empty;
			if(lineItem.ExtendedData != null && lineItem.ExtendedData.ContainsKey(AvaTaxConstants.ITEM_CODE_KEY)) 
			{
				avaItemCode = lineItem.ExtendedData.GetValue(AvaTaxConstants.ITEM_CODE_KEY);
			}

			if (string.IsNullOrEmpty(avaItemCode))
			{
				avaItemCode = GetAvaTaxCodeForLineItem(lineItem);
				
			}

			// TODO DEV TESTING ONLY, temporary Item Code value until they are added to the SMO dashboard Product DocType
			if (string.IsNullOrEmpty(avaItemCode))
			{
				avaItemCode = "testItemCode123_TODO";
			}

			// Identify AvaTax Line object for the current Invoice lineItem
			TransactionLineModel avaLine = tm.lines.FirstOrDefault(x => x.itemCode == avaItemCode);

			if(avaLine == null)
			{
				Exception ex = new Exception("No AvaTax line model found for itemCode " + avaItemCode + " in lineItem " + lineItem.Key.ToString());
				LogHelper.Error(typeof(AvaTaxLineItemVisitor), "Error finding AvaTax line for invoice lineItem. ", ex);

				// TODO How to handle a failed line item?

				return;
			}
			
			// TODO, Finish discount tax when Discount lineItems are introduced
			if (lineItem.LineItemType == LineItemType.Discount)
			{				
				//lineItem.ExtendedData.SetValue(Core.Constants.ExtendedDataKeys.LineItemTaxAmount, (-lineItem.TotalPrice * this._taxRate).ToString(CultureInfo.InvariantCulture));
			}
			else
			{
				//avaLine.tax // Applies 3rd party overrides, may differ from taxCalculated
				//avaLine.taxCalculated // Does not apply any 3rd party overrides outside of AvaTax. This is the value that should be used for v1.0
				decimal lineTax = avaLine.taxCalculated ?? 0;
				if (avaLine.taxCalculated == null)
				{
					// TODO Don't assume NULL taxCalculated should remain as 0
					Exception ex = new Exception("AvaTax calculated tax is NULL for itemCode " + avaItemCode + " in lineItem " + lineItem.Key.ToString());
					LogHelper.Error(typeof(AvaTaxLineItemVisitor), "Error retrieving calculated tax from AvaTax response for invoice lineItem. ", ex);

					return;
				}
				
				// Set specific ED value eComm expects for LineItemTaxAmount 
				lineItem.ExtendedData.SetValue(Core.Constants.ExtendedDataKeys.LineItemTaxAmount, lineTax.ToString(CultureInfo.InvariantCulture));

				// Save entire avaLine data to ED collection if needed for later reference(s)
				lineItem.ExtendedData.SetValue(AvaTaxConstants.TRANSACTION_LINE, JsonConvert.SerializeObject(avaLine)); // TODO Confirm serialization succeeds with full test data

				/* Some available properties that may be helpful
					avaLine.details[#].rate -- one details object per tax authority
					avaLine.tax
					avaLine.taxableAmount
					avaLine.taxCode
					avaLine.taxCodeId
					avaLine.taxEngine
				*/
				// TODO There are going to be MULTIPLE objects in the detail collection...how should a baseTaxRate be determined?
				if (avaLine.details != null && avaLine.details.Count == 1)
				{
					lineItem.ExtendedData.SetValue(Core.Constants.ExtendedDataKeys.BaseTaxRate, avaLine.details.First().rate.ToString());
				}				
			}
			
			_lineItems.Add(lineItem);
		}

		private string GetAvaTaxCodeForLineItem(ILineItem lineItem)
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
			if(pv.DetachedContents != null && pv.DetachedContents.Any())
			{
				// TODO v.1.5 Pass this DocType value from front-end project instead of having the Tax provider inherintely know about it
				taxCode = pv.DetachedContents.First().DetachedDataValues.FirstOrDefault(x => x.Key == "avaTaxProductCode").Value; // TODO AvaTaxConstants.AvaTaxProductCodeDocTypePropertyAlias
			}

			return taxCode;
		}
	}
}
