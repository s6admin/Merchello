namespace Merchello.Core.Gateways.Taxation
{
    using Merchello.Core.Models;

    using Umbraco.Core;

    /// <summary>
    /// Represents the abstract GatewayTaxMethod
    /// </summary>
    public abstract class TaxationGatewayMethodBase
    {
        /// <summary>
        /// The tax method.
        /// </summary>
        private readonly ITaxMethod _taxMethod;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaxationGatewayMethodBase"/> class.
        /// </summary>
        /// <param name="taxMethod">
        /// The tax method.
        /// </param>
        protected TaxationGatewayMethodBase(ITaxMethod taxMethod)
        {
            Mandate.ParameterNotNull(taxMethod, "taxMethod");

            _taxMethod = taxMethod;
        }

        /// <summary>
        /// Gets the <see cref="ITaxMethod"/>
        /// </summary>
        public ITaxMethod TaxMethod
        {
            get { return _taxMethod; }
        }

        /// <summary>
        /// Calculates the tax amount for an invoice
        /// </summary>
        /// <param name="invoice">The <see cref="IInvoice"/></param>
        /// <returns>The <see cref="ITaxCalculationResult"/></returns>
        /// <remarks>
        /// 
        /// Assumes the billing address of the invoice will be used for the taxation address
        /// 
        /// </remarks>
        public virtual ITaxCalculationResult CalculateTaxForInvoice(IInvoice invoice)
        {
            return CalculateTaxForInvoice(invoice, invoice.GetBillingAddress());
        }

        /// <summary>
        /// Calculates the tax amount for an invoice
        /// </summary>
        /// <param name="invoice">The <see cref="IInvoice"/></param>
        /// <param name="taxAddress">The <see cref="IAddress"/> to base taxation rates.  Either origin or destination address.</param>
        /// <returns><see cref="ITaxCalculationResult"/></returns>
        public abstract ITaxCalculationResult CalculateTaxForInvoice(IInvoice invoice, IAddress taxAddress);

		/// <summary>
		/// S6 Custom abstract method for calculating invoice taxes using a third party service which requires authentication.
		/// </summary>
		/// <param name="invoice">The invoice.</param>
		/// <param name="taxAddress">The tax address.</param>
		/// <param name="user">The user.</param>
		/// <param name="pswd">The PSWD.</param>
		/// <returns></returns>
		public abstract ITaxCalculationResult CalculateTaxForInvoice(IInvoice invoice, IAddress taxAddress, string user, string pswd);

        /// <summary>
        /// Calculates the tax amount for an invoice
        /// </summary>
        /// <param name="strategy">The strategy to use when calculating the tax amount</param>
        /// <returns><see cref="ITaxCalculationResult"/></returns>
        public virtual ITaxCalculationResult CalculateTaxForInvoice(ITaxCalculationStrategy strategy)
        {
            var attempt = strategy.CalculateTaxesForInvoice();

            if (!attempt.Success) throw attempt.Exception;

            return attempt.Result;
        }

		public virtual ITaxCalculationResult CalculateTaxForInvoice(ITaxCalculationStrategy strategy, string user, string pswd)
		{
			var attempt = strategy.CalculateTaxesForInvoice(user, pswd);

			if (!attempt.Success) throw attempt.Exception;

			return attempt.Result;
		}
    }
}