namespace Merchello.Providers.Payment.PayTrace
{
    using System;

    using Merchello.Core.Models;
    using Merchello.Providers.Payment.PayTrace.Models;
    using Merchello.Providers.Payment.PayTrace.Services;

    using Umbraco.Core;

    /// <summary>
	/// The PayTrace payment processor
	/// </summary>
	internal class PayTraceCheckoutPaymentProcessor
	{
        /// <summary>
        /// The <see cref="IPayTraceApiPaymentService"/>.
        /// </summary>
        private readonly PayTraceCheckoutService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="PayTraceCheckoutPaymentProcessor"/> class.
        /// </summary>
        /// <param name="service">
        /// The <see cref="IPayTraceApiPaymentService"/>.
        /// </param>
        public PayTraceCheckoutPaymentProcessor(IPayTraceApiService service)
        {
            Mandate.ParameterNotNull(service, "service");
			this._service = (PayTraceCheckoutService)service.Checkout;
        }

        /// <summary>
        /// Verifies the authorization of a success return.
        /// </summary>
        /// <param name="invoice">
        /// The invoice.
        /// </param>
        /// <param name="payment">
        /// The payment.
        /// </param>
        /// <returns>
        /// The <see cref="PayTraceTransactionRecord"/>.
        /// </returns>
        public PayTraceTransactionRecord VerifySuccessAuthorziation(IInvoice invoice, IPayment payment)
        {
            // We need to process several transactions in a row to get all the data we need to record the
            // transaction with enough information to do refunds / partial refunds
            var record = payment.GetPayTraceTransactionRecord();
            if (record == null || record.SetCheckout == null || record.Data.Token.IsNullOrWhiteSpace())
            {
                throw new NullReferenceException("PayTrace  Checkout must be setup");
            }

            record = _service.GetCheckoutDetails(payment, record.Data.Token, record);
            if (!record.Success) return record;

            record = Process(payment, _service.DoCheckoutPayment(invoice, payment, record.Data.Token, record.Data.PayerId, record));
            if (!record.Success) return record;

            record = Process(payment, _service.Authorize(invoice, payment, record.Data.Token, record.Data.PayerId, record));
            return record;
        }

        /// <summary>
        /// Processes the payment.
        /// </summary>
        /// <param name="payment">
        /// The payment.
        /// </param>
        /// <param name="record">
        /// The record.
        /// </param>
        /// <returns>
        /// The <see cref="PayTraceTransactionRecord"/>.
        /// </returns>
        private PayTraceTransactionRecord Process(IPayment payment, PayTraceTransactionRecord record)
        {
            payment.SavePayTraceTransactionRecord(record);
            return record;
        }
	}
}
