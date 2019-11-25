using Merchello.Core.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;

namespace Merchello.Core.Gateways.Taxation.AvaTax
{
	internal class AvaTaxCalculationStrategy : TaxCalculationStrategyBase
	{
		private readonly ITaxMethod _taxMethod;

		public AvaTaxCalculationStrategy(IInvoice invoice, IAddress taxAddress, ITaxMethod taxMethod)
			: base(invoice, taxAddress)
		{
			Mandate.ParameterNotNull(taxMethod, "countryTaxRate");
			_taxMethod = taxMethod;
		}

		// S6 TODO Finish (copied from FixedRate)
		public override Attempt<ITaxCalculationResult> CalculateTaxesForInvoice()
		{
			var extendedData = new ExtendedDataCollection();

			try
			{
				var baseTaxRate = _taxMethod.PercentageTaxRate;

				// S6 TODO Is BaseTaxRate going to be the AvaTax Sales Order (estimate) result?
				extendedData.SetValue(Core.Constants.ExtendedDataKeys.BaseTaxRate, baseTaxRate.ToString(CultureInfo.InvariantCulture));

				if (_taxMethod.HasProvinces)
				{
					baseTaxRate = AdjustedRate(baseTaxRate, _taxMethod.Provinces.FirstOrDefault(x => x.Code == TaxAddress.Region), extendedData);
				}

				// S6 Visitor applied to each line item. AvaTax response is for the entire basket so we may need to distribute the result to each line Item
				var visitor = new TaxableLineItemVisitor(baseTaxRate / 100);

				Invoice.Items.Accept(visitor);
				
				var totalTax = visitor.TaxableLineItems.Sum(x => decimal.Parse(x.ExtendedData.GetValue(Core.Constants.ExtendedDataKeys.LineItemTaxAmount), CultureInfo.InvariantCulture));
				
				return Attempt<ITaxCalculationResult>.Succeed(
					new TaxCalculationResult(_taxMethod.Name, baseTaxRate, totalTax, extendedData));
			}
			catch (Exception ex)
			{
				return Attempt<ITaxCalculationResult>.Fail(ex);
			}
		}

		// S6 TODO Finish (copied from FixedRate)
		private static decimal AdjustedRate(decimal baseRate, ITaxProvince province, ExtendedDataCollection extendedData)
		{
			if (province == null) return baseRate;
			extendedData.SetValue(Core.Constants.ExtendedDataKeys.ProviceTaxRate, province.PercentAdjustment.ToString(CultureInfo.InvariantCulture));
			return province.PercentAdjustment + baseRate;
		}
	}
}
