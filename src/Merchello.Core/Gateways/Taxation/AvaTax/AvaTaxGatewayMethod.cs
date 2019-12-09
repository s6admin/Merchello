using Merchello.Core.Gateways.Taxation.AvaTax;
using Merchello.Core.Configuration;
using Merchello.Core.Models;

using Umbraco.Core.Logging;
using System;

namespace Merchello.Core.Gateways.Taxation.AvaTax
{
	/// <summary>
	/// AvaTax taxation gateway method.
	/// </summary>
	/// <seealso cref="Merchello.Core.Gateways.Taxation.TaxationGatewayMethodBase" />
	/// <seealso cref="Merchello.Core.Gateways.Taxation.AvaTax.IAvaTaxGatewayMethod" />
	public class AvaTaxGatewayMethod : TaxationGatewayMethodBase, IAvaTaxGatewayMethod
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AvaTaxGatewayMethod"/> class.
		/// </summary>
		/// <param name="taxMethod">The tax method.</param>
		public AvaTaxGatewayMethod(ITaxMethod taxMethod)
			:base(taxMethod)
		{}

		/// <summary>
		/// S6 Required override method for interface. Do NOT use this method as AvaTax requires authentication. Use <see cref="CalculateTaxForInvoice(IInvoice, IAddress, string, string)"/> method instead. 
		/// </summary>
		/// <param name="invoice">The <see cref="IInvoice" /></param>
		/// <param name="taxAddress">The <see cref="IAddress" /> to base taxation rates.  Either origin or destination address.</param>
		/// <param name="quoteOnly">Indicates if the tax calculations are an estimate (true) or final (false). AvaTax provider default is set to true so all calculations are considered SalesOrder estimates instead of final SalesInvoice records.</param>
		/// <returns>
		///   <see cref="ITaxCalculationResult" />
		/// </returns>
		public override ITaxCalculationResult CalculateTaxForInvoice(IInvoice invoice, IAddress taxAddress, bool quoteOnly = true)
		{

			var ctrValues = new object[] { invoice, taxAddress, TaxMethod };

			var typeName = MerchelloConfiguration.Current.GetStrategyElement(Core.Constants.StrategyTypeAlias.DefaultInvoiceTaxRateQuote).Type;

			var attempt = ActivatorHelper.CreateInstance<TaxCalculationStrategyBase>(typeName, ctrValues);

			if (!attempt.Success)
			{
				LogHelper.Error<AvaTaxGatewayProvider>("Failed to instantiate the tax calculation strategy '" + typeName + "'", attempt.Exception);
				throw attempt.Exception;
			}

			return CalculateTaxForInvoice(attempt.Result, quoteOnly);			
		}
		
		/// <summary>
		/// Calculates the tax for product.
		/// </summary>
		/// <param name="product">The product.</param>
		/// <returns></returns>
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
