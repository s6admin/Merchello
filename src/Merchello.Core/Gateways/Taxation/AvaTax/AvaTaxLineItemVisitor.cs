using Merchello.Core.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
		private readonly decimal _taxRate;

		/// <summary>
		/// Initializes a new instance of the <see cref="TaxableLineItemVisitor"/> class.
		/// </summary>
		/// <param name="taxRate">
		/// The tax rate.
		/// </param>
		public AvaTaxLineItemVisitor(decimal taxRate)
		{
			_taxRate = taxRate > 1 ? taxRate / 100 : taxRate;
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
			if (!lineItem.ExtendedData.GetTaxableValue()) return;
			if (lineItem.LineItemType == LineItemType.Discount)
			{
				lineItem.ExtendedData.SetValue(Core.Constants.ExtendedDataKeys.LineItemTaxAmount, (-lineItem.TotalPrice * this._taxRate).ToString(CultureInfo.InvariantCulture));
			}
			else
			{
				lineItem.ExtendedData.SetValue(Core.Constants.ExtendedDataKeys.LineItemTaxAmount, (lineItem.TotalPrice * this._taxRate).ToString(CultureInfo.InvariantCulture));
			}
			// S6 TODO Is BaseTaxRate assumed 0 or do we wait for AvaTax response to populate the value dynamically?
			lineItem.ExtendedData.SetValue(Core.Constants.ExtendedDataKeys.BaseTaxRate, this._taxRate.ToString());
			_lineItems.Add(lineItem);
		}
	}
}
