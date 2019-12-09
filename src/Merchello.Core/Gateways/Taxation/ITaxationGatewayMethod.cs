namespace Merchello.Core.Gateways.Taxation
{
    using Merchello.Core.Models;

    /// <summary>
    /// Defines the abstract GatewayTaxMethod
    /// </summary>
    public interface ITaxationGatewayMethod : IGatewayMethod
    {
        /// <summary>
        /// Gets the <see cref="ITaxationGatewayMethod"/>
        /// </summary>
        ITaxMethod TaxMethod { get; }

        /// <summary>
        /// Calculates the tax amount for an invoice
        /// </summary>
        /// <param name="invoice">
        /// The <see cref="IInvoice"/>
        /// </param>
        /// <returns>
        /// The <see cref="ITaxCalculationResult"/>
        /// </returns>
        /// <remarks>
        /// 
        /// Assumes the billing address of the invoice will be used for the taxation address
        /// 
        /// </remarks>
        ITaxCalculationResult CalculateTaxForInvoice(IInvoice invoice);

        /// <summary>
        /// Calculates the tax amount for an invoice
        /// </summary>
        /// <param name="invoice">
        /// The <see cref="IInvoice"/>
        /// </param>
        /// <param name="taxAddress">
        /// The <see cref="IAddress"/> to base taxation rates.  Either origin or destination address.
        /// </param>
        /// <returns>
        /// The <see cref="ITaxCalculationResult"/>
        /// </returns>
        ITaxCalculationResult CalculateTaxForInvoice(IInvoice invoice, IAddress taxAddress);

        /// <summary>
        /// Calculates the tax amount for an invoice
        /// </summary>
        /// <param name="strategy">
        /// The strategy to use when calculating the tax amount
        /// </param>
        /// <returns>
        /// The <see cref="ITaxCalculationResult"/>
        /// </returns>
        ITaxCalculationResult CalculateTaxForInvoice(ITaxCalculationStrategy strategy);

		/// <summary>
		/// S6 Custom override method for calculating invoice taxes that use a third party service which requires authentication.
		/// </summary>
		/// <param name="invoice">The invoice.</param>
		/// <param name="taxAddress">The tax address.</param>
		/// <param name="user">The user.</param>
		/// <param name="pswd">The PSWD.</param>
		/// <returns></returns>
		//ITaxCalculationResult CalculateTaxForInvoice(IInvoice invoice, IAddress taxAddress, string user, string pswd);
	}
}