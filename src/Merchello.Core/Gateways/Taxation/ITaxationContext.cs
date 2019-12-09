﻿namespace Merchello.Core.Gateways.Taxation
{
    using Models;

    /// <summary>
    /// Defines the Taxation context
    /// </summary>
    public interface ITaxationContext : IGatewayProviderTypedContextBase<TaxationGatewayProviderBase>
    {
        /// <summary>
        /// Gets a value indicating whether product pricing enabled.
        /// </summary>
        bool ProductPricingEnabled { get; }

        /// <summary>
        /// Gets the taxation application.
        /// </summary>
        TaxationApplication TaxationApplication { get; }

        /// <summary>
        /// Gets the <see cref="ITaxationByProductMethod"/>.
        /// </summary>
        ITaxationByProductMethod ProductPricingTaxMethod { get; }

		string TaxationProviderUsername { get; set; }

		string TaxationProviderPassword { get; set; }

        /// <summary>
        /// Calculates taxes for the <see cref="IInvoice"/>
        /// </summary>
        /// <param name="invoice">The <see cref="IInvoice"/> to tax</param>
        /// <param name="quoteOnly">A value indicating whether or not the taxes should be calculated as a quote</param>
        /// <returns>The <see cref="ITaxCalculationResult"/></returns>
        /// <remarks>
        /// 
        /// This assumes that the tax rate is assoicated with the invoice's billing address
        /// 
        /// </remarks>
        ITaxCalculationResult CalculateTaxesForInvoice(IInvoice invoice, bool quoteOnly = false);

        /// <summary>
        /// Calculates taxes for the <see cref="IInvoice"/>
        /// </summary>
        /// <param name="invoice">
        /// The <see cref="IInvoice"/> to tax
        /// </param>
        /// <param name="taxAddress">
        /// The address to base the taxation calculation - generally country and region
        /// </param>
        /// <param name="quoteOnly">
        /// A value indicating whether or not the taxes should be calculated as a quote
        /// </param>
        /// <returns>
        /// The <see cref="ITaxCalculationResult"/>
        /// </returns>
        ITaxCalculationResult CalculateTaxesForInvoice(IInvoice invoice, IAddress taxAddress, bool quoteOnly = false);
		
		/// <summary>
		/// The calculate taxes for a product.
		/// </summary>
		/// <param name="product">
		/// The product.
		/// </param>
		/// <returns>
		/// The <see cref="ITaxCalculationResult"/>.
		/// </returns>
		IProductTaxCalculationResult CalculateTaxesForProduct(IProductVariantDataModifierData product);

        /// <summary>
        /// Gets the tax method for a given tax address
        /// </summary>
        /// <param name="taxAddress">
        /// The tax address
        /// </param>
        /// <returns>
        /// The <see cref="ITaxMethod"/>.
        /// </returns>
        ITaxMethod GetTaxMethodForTaxAddress(IAddress taxAddress);

        /// <summary>
        /// Gets the tax method for country code.
        /// </summary>
        /// <param name="countryCode">
        /// The country code.
        /// </param>
        /// <returns>
        /// The <see cref="ITaxMethod"/>.
        /// </returns>
        ITaxMethod GetTaxMethodForCountryCode(string countryCode);
    }
}