﻿namespace Merchello.Core.Gateways.Taxation.FixedRate
{
	using System;
	using System.Globalization;
	using System.Linq;

	using Merchello.Core.Configuration;
	using Merchello.Core.Models;

	using Umbraco.Core.Logging;

	/// <summary>
	/// The fix rate taxation gateway method.
	/// </summary>
	public class FixRateTaxationGatewayMethod : TaxationGatewayMethodBase, IFixedRateTaxationGatewayMethod
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FixRateTaxationGatewayMethod"/> class.
        /// </summary>
        /// <param name="taxMethod">
        /// The tax method.
        /// </param>
        public FixRateTaxationGatewayMethod(ITaxMethod taxMethod)
            : base(taxMethod)
        {
        }

        /// <summary>
        /// The calculate tax for invoice.
        /// </summary>
        /// <param name="invoice">
        /// The invoice.
        /// </param>
        /// <param name="taxAddress">
        /// The tax address.
        /// </param>
        /// <returns>
        /// The <see cref="ITaxCalculationResult"/>.
        /// </returns>
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

		// S6 FixedRate Tax provider does not use authentication, but method signature is required 
		public override ITaxCalculationResult CalculateTaxForInvoice(IInvoice invoice, IAddress taxAddress, string user, string pswd)
		{
			return CalculateTaxForInvoice(invoice, taxAddress);
		}


		/// <summary>
		/// Calculates taxes for a product.
		/// </summary>
		/// <param name="product">
		/// The <see cref="IProductVariantDataModifierData"/>.
		/// </param>
		/// <returns>
		/// The <see cref="ITaxCalculationResult"/>.
		/// </returns>
		public virtual IProductTaxCalculationResult CalculateTaxForProduct(IProductVariantDataModifierData product)
        {            
            var baseTaxRate = TaxMethod.PercentageTaxRate;

            var taxRate = baseTaxRate > 1 ? baseTaxRate / 100M : baseTaxRate;

            var priceCalc = product.Price * taxRate;

            var salePriceCalc = product.SalePrice * taxRate;

            return new ProductTaxCalculationResult(TaxMethod.Name, product.Price, priceCalc, product.SalePrice, salePriceCalc, baseTaxRate);
        }
    }
}