namespace Merchello.Providers.Payment.PayTrace.Models
{
    using Merchello.Core.Models;

    //using global::PayTrace.PayTraceAPIInterfaceService.Model;

    /// <summary>
    /// An event model that allows for overriding default PayTrace Checkout settings.
    /// </summary>
    public class PayTraceCheckoutRequestDetailsOverride
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PayTraceCheckoutRequestDetailsOverride"/> class.
        /// </summary>
        /// <param name="invoice">
        /// The invoice.
        /// </param>
        /// <param name="payment">
        /// The payment.
        /// </param>
        /// <param name="ecDetails">
        /// The  checkout details.
        /// </param>
        public PayTraceCheckoutRequestDetailsOverride(IInvoice invoice, IPayment payment, SetCheckoutRequestDetailsType ecDetails)
        {
            this.Invoice = invoice;
            this.Payment = payment;
            this.CheckoutDetails = ecDetails;
        }

        /// <summary>
        /// Gets the <see cref="IInvoice"/>.
        /// </summary>
        public IInvoice Invoice { get; private set; }

        /// <summary>
        /// Gets the <see cref="IPayment"/>.
        /// </summary>
        public IPayment Payment { get; private set; }

        /// <summary>
        /// Gets or sets the <see cref="SetCheckoutRequestDetailsType"/>.
        /// </summary>
        public SetCheckoutRequestDetailsType CheckoutDetails { get; set; }
    }
}