namespace Merchello.Core.Gateways.Taxation
{
    using Merchello.Core.Strategies;

    using Umbraco.Core;

    /// <summary>
    /// Defines a taxation strategy
    /// </summary>
    public interface ITaxCalculationStrategy : IStrategy
    {
        /// <summary>
        /// Computes the invoice tax result
        /// </summary>
        /// <returns>The <see cref="ITaxCalculationResult"/></returns>
        Attempt<ITaxCalculationResult> CalculateTaxesForInvoice(bool quoteOnly = false);
		
		/// <summary>
		/// S6 Calculates the taxes for invoice using third party service requiring authentication.
		/// </summary>
		/// <param name="user">The user.</param>
		/// <param name="pswd">The PSWD.</param>
		/// <returns></returns>
		Attempt<ITaxCalculationResult> CalculateTaxesForInvoice(string user, string pswd, bool quoteOnly = false);
    }
}
