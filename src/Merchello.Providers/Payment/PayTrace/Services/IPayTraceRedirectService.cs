using Merchello.Core.Models;

using Merchello.Providers.Payment.PayTrace.Models;

using Umbraco.Core.Services;

namespace Merchello.Providers.Payment.PayTrace.Services
{
	public interface IPayTraceRedirectService : IService
	{
		/// <summary>
		/// Performs the setup for a checkout.
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
		PayTraceRedirectTransactionRecord SetCheckout(IInvoice invoice, IPayment payment);
		
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
		/// The <see cref="PayTraceResponse"/>.
		/// </returns>
		PayTraceRedirectTransactionRecord Capture(IInvoice invoice, IPayment payment, decimal amount, bool isPartialPayment);

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
		/// The <see cref="PayTraceRedirectTransactionRecord"/>.
		/// </returns>
		PayTraceRedirectResponse Refund(IInvoice invoice, IPayment payment, decimal amount);
	}
}
