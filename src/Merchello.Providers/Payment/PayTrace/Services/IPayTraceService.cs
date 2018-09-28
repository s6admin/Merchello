namespace Merchello.Providers.Payment.PayTrace.Services
{
    using Merchello.Core.Models;

    using Merchello.Providers.Payment.PayTrace.Models;

    using Umbraco.Core.Services;

    /// <summary>
    /// Defines a PayTraceCheckoutService.
    /// </summary>
    public interface IPayTraceCheckoutService : IService
    {
        /// <summary>
        /// Performs the setup for an  checkout.
        /// </summary>
        /// <param name="invoice">
        /// The <see cref="IInvoice"/>.
        /// </param>
        /// <param name="payment">
        /// The <see cref="IPayment"/>
        /// </param>
        /// <returns>
        /// The <see cref="CheckoutResponse"/>.
        /// </returns>
        PayTraceTransactionRecord SetCheckout(IInvoice invoice, IPayment payment);


        /// <summary>
        /// The capture success.
        /// </summary>
        /// <param name="invoice">
        /// The invoice.
        /// </param>
        /// <param name="payment">
        /// The payment.
        /// </param>
        /// <param name="amount">
        /// The amount.
        /// </param>
        /// <param name="isPartialPayment">
        /// The is partial payment.
        /// </param>
        /// <returns>
        /// The <see cref="CheckoutResponse"/>.
        /// </returns>
        PayTraceTransactionRecord Capture(IInvoice invoice, IPayment payment, decimal amount, bool isPartialPayment);

        /// <summary>
        /// Refunds or partially refunds a payment.
        /// </summary>
        /// <param name="invoice">
        /// The invoice.
        /// </param>
        /// <param name="payment">
        /// The payment.
        /// </param>
        /// <param name="amount">
        /// The amount of the refund.
        /// </param>
        /// <returns>
        /// The <see cref="PayTraceTransactionRecord"/>.
        /// </returns>
        CheckoutResponse Refund(IInvoice invoice, IPayment payment, decimal amount);
    }
} 