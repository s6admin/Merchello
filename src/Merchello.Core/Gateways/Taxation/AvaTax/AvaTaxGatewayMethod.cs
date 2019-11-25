using Merchello.Core.Gateways.Taxation.FixedRate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Linq;

using Merchello.Core.Configuration;
using Merchello.Core.Models;

using Umbraco.Core.Logging;

namespace Merchello.Core.Gateways.Taxation.AvaTax
{
	/// <summary>
	/// AvaTax gateway method.
	/// </summary>
	/// <seealso cref="Merchello.Core.Gateways.Taxation.TaxationGatewayMethodBase" />
	/// <seealso cref="Merchello.Core.Gateways.Taxation.FixedRate.IFixedRateTaxationGatewayMethod" />
	public class AvaTaxGatewayMethod : TaxationGatewayMethodBase, IFixedRateTaxationGatewayMethod
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AvaTaxGatewayMethod"/> class.
		/// </summary>
		/// <param name="taxMethod">The tax method.</param>
		public AvaTaxGatewayMethod(ITaxMethod taxMethod)
			:base(taxMethod)
		{}

		public override ITaxCalculationResult CalculateTaxForInvoice(IInvoice invoice, IAddress taxAddress)
		{
			var ctrValues = new object[] { invoice, taxAddress, TaxMethod };

			var typeName = MerchelloConfiguration.Current.GetStrategyElement(Constants.StrategyTypeAlias.DefaultInvoiceTaxRateQuote).Type;

			var attempt = ActivatorHelper.CreateInstance<TaxCalculationStrategyBase>(typeName, ctrValues);

			if (!attempt.Success)
			{
				LogHelper.Error<FixedRateTaxationGatewayProvider>("Failed to instantiate the tax calculation strategy '" + typeName + "'", attempt.Exception);
				throw attempt.Exception;
			}

			return CalculateTaxForInvoice(attempt.Result);
		}

		public virtual IProductTaxCalculationResult CalculateTaxForProduct(IProductVariantDataModifierData product)
		{
			decimal baseTaxRate = TaxMethod.PercentageTaxRate; // TODO
			decimal priceCalc = 0; // TODO

			decimal salePriceCalc = 0; // TODO

			// TODO baseTaxRate (optional) parameter?
			return new ProductTaxCalculationResult(TaxMethod.Name, product.Price, priceCalc, product.SalePrice, salePriceCalc);

			#region Example from FlatRate provider

			/*var baseTaxRate = TaxMethod.PercentageTaxRate;

			var taxRate = baseTaxRate > 1 ? baseTaxRate / 100M : baseTaxRate;

			var priceCalc = product.Price * taxRate;

			var salePriceCalc = product.SalePrice * taxRate;

			return new ProductTaxCalculationResult(TaxMethod.Name, product.Price, priceCalc, product.SalePrice, salePriceCalc, baseTaxRate);*/

			#endregion Example from FlatRate provider
		}
	}
}
