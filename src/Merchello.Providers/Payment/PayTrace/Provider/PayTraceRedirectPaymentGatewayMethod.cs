using System;
using System.Linq;

using Merchello.Core;
using Merchello.Core.Gateways;
using Merchello.Core.Gateways.Payment;
using Merchello.Core.Models;
using Merchello.Core.Services;
using Merchello.Providers.Exceptions;
//using Merchello.Providers.Payment.PayTrace.Models;
//using Merchello.Providers.Payment.PayTrace.Services;

using Umbraco.Core;

using StringExtensions = Merchello.Core.StringExtensions;
using Merchello.Providers.Payment.PayTrace.Services;
using Merchello.Providers.Payment.PayTrace.Models;
using static Merchello.Providers.Constants.PayTraceRedirect;

namespace Merchello.Providers.Payment.PayTrace.Provider
{

	/// <summary>
	/// A payment method for facilitating PayTrace Checkouts.
	/// </summary>
	[GatewayMethodUi("PayTrace.RedirectCheckout")]
	[GatewayMethodEditor("PayTrace Redirect Method Editor", "PayTrace Redirect Checkout", "~/App_Plugins/Merchello/Backoffice/Merchello/Dialogs/payment.paymentmethod.addedit.html")]
	[PaymentGatewayMethod("PayTrace Redirect Method Editors",
		"",
		"",
		"~/App_Plugins/MerchelloProviders/views/dialogs/voidpayment.confirm.html",
		"~/App_Plugins/MerchelloProviders/views/dialogs/refundpayment.confirm.html")]
	public class PayTraceRedirectPaymentGatewayMethod : RedirectPaymentMethodBase
	{
		private readonly IPayTraceRedirectService _paytraceService;

		/// <summary>
		/// Initializes a new instance of the <see cref="PayTraceCheckoutPaymentGatewayMethod"/> class.
		/// </summary>
		/// <param name="gatewayProviderService">
		/// The gateway provider service.
		/// </param>
		/// <param name="paymentMethod">
		/// The payment method.
		/// </param>
		/// <param name="PayTraceApiService">
		/// The <see cref="IPayTraceApiService"/>.
		/// </param>
		public PayTraceRedirectPaymentGatewayMethod(IGatewayProviderService gatewayProviderService, IPaymentMethod paymentMethod, IPayTraceRedirectService paytraceService)
            : base(gatewayProviderService, paymentMethod)
        {
			Ensure.ParameterNotNull(paytraceService, "PayTraceRedirectService");
			this._paytraceService = paytraceService;
		}
		
		protected override IPaymentResult PerformAuthorizeCapturePayment(IInvoice invoice, decimal amount, ProcessorArgumentCollection args)
		{
			throw new NotImplementedException();
		}

		public override IPaymentResult AuthorizePayment(IInvoice invoice, ProcessorArgumentCollection args)
		{
			return base.AuthorizePayment(invoice, args);
		}

		/// <summary>
		/// Performs the AuthorizePayment operation.
		/// </summary>
		/// <param name="invoice">
		/// The invoice.
		/// </param>
		/// <param name="args">
		/// The <see cref="ProcessorArgumentCollection"/>.
		/// </param>
		/// <returns>
		/// The <see cref="IPaymentResult"/>.
		/// </returns>
		/// <remarks>
		/// For the Checkout there is not technically an "Authorize" but we use this to start the checkout process and to 
		/// mark intent to pay before redirecting the customer to PayTrace.  e.g.  This method is called after the customer has
		/// clicked the Pay button, we then save the invoice and "Authorize" a payment setting the invoice status to Unpaid before redirecting.
		/// IN this way, we have both an Invoice and a Payment (denoting the redirect).  When the customer completes the purchase on PayTrace sites
		/// the payment will be used to perform a capture and the invoice status will be changed to Paid.  In the event the customer cancels,
		/// the invoice will either be voided or deleted depending on the configured setting.  
		/// Events are included in the controller handling the response to allow developers to override success and cancel redirect URLs.
		/// </remarks>
		protected override IPaymentResult PerformAuthorizePayment(IInvoice invoice, ProcessorArgumentCollection args)
		{
			var authorizeAmount = invoice.Total;
			if (args.ContainsKey("authorizePaymentAmount"))
			{
				authorizeAmount = Convert.ToDecimal(args["authorizePaymentAmount"]);
			}

			var payment = GatewayProviderService.CreatePayment(PaymentMethodType.Redirect, authorizeAmount, PaymentMethod.Key);
			payment.CustomerKey = invoice.CustomerKey;
			payment.PaymentMethodName = PaymentMethod.Name;
			payment.ReferenceNumber = PaymentMethod.PaymentCode + "-" + invoice.PrefixedInvoiceNumber();
			payment.Collected = false;
			payment.Authorized = false; // this is technically not the authorization.  We'll mark this in a later step.

			// S6 Set Billing Name during authorization since its available and so it doesn't need to be fetched during the post-redirect Capture
			payment.ExtendedData.SetValue(ProcessorArgumentsKeys.BillingAddressName, invoice.BillToName);
			payment.ExtendedData.SetValue(ExtendedDataKeys.PayTraceTransaction, "1"); // S6 Simply used to help identify the type of data in the payment object on the front-end website 
			
			// Have to save here to generate the payment key
			GatewayProviderService.Save(payment);

			// Now we want to get things setup for the Checkout
			var record = this._paytraceService.SetCheckout(invoice, payment);
			payment.SavePayTraceTransactionRecord(record);

			// Have to save here to persist the record so it can be used in later processing.
			GatewayProviderService.Save(payment);
						
			// We want to do our own Apply Payment operation as the amount has not been collected -
			// so we create an applied payment with a 0 amount.  Once the payment has been "collected", another Applied Payment record will
			// be created showing the full amount and the invoice status will be set to Paid.
			GatewayProviderService.ApplyPaymentToInvoice(payment.Key, invoice.Key, AppliedPaymentType.Debit, string.Format("To show promise of a {0} payment via PayTrace Checkout", PaymentMethod.Name), 0);
						
			if (record.Success)
			{
				return new PaymentResult(Attempt<IPayment>.Succeed(payment), invoice, false); // ,record.SetCheckout.RedirectUrl
			}

			// In the case of a failure, package up the exception so we can bubble it up.
			var ex = new PayTraceRedirectApiException("PayTrace Checkout initial response failed.");			
			//if (record.SetCheckout.ErrorTypes.Any()) ex.ErrorTypes = record.SetCheckout.ErrorTypes;

			return new PaymentResult(Attempt<IPayment>.Fail(payment, ex), invoice, false);
		}

		public string GetPaymentReferenceNumber(IInvoice invoice)
		{
			return PaymentMethod.PaymentCode + "-" + invoice.PrefixedInvoiceNumber();
		}

		/// <summary>
		/// Performs the capture payment operation.
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
		/// <param name="args">
		/// The args.
		/// </param>
		/// <returns>
		/// The <see cref="IPaymentResult"/>.
		/// </returns>
		protected override IPaymentResult PerformCapturePayment(IInvoice invoice, IPayment payment, decimal amount, ProcessorArgumentCollection args)
		{
			// We need to determine if the entire amount authorized has been collected before marking
			// the payment collected.
			var appliedPayments = GatewayProviderService.GetAppliedPaymentsByPaymentKey(payment.Key);
			var applied = appliedPayments.Sum(x => x.Amount);

			var isPartialPayment = amount - applied <= 0;

			var processor = new PayTraceRedirectProcessor(_paytraceService);
			var record = processor.VerifySuccessAuthorziation(invoice, payment);

			if (record.Success)
			{
				record = _paytraceService.Capture(invoice, payment, amount, isPartialPayment); // This appears to be a Capture on PayPal's end and returns a transactionId. Nothing applicable to PayTrace so far.
				payment.SavePayTraceTransactionRecord(record);

				if (record.Success)
				{										
		
					payment.Collected = (amount + applied) == payment.Amount;
					payment.Authorized = true;

					GatewayProviderService.Save(payment);

					GatewayProviderService.ApplyPaymentToInvoice(payment.Key, invoice.Key, AppliedPaymentType.Debit, "PayTrace Checkout SUCCESS payment", amount);

					return new PaymentResult(Attempt<IPayment>.Succeed(payment), invoice, CalculateTotalOwed(invoice).CompareTo(amount) <= 0);
				}

				GatewayProviderService.Save(payment);
			}

			return new PaymentResult(Attempt<IPayment>.Fail(payment), invoice, false);
		}

		/// <summary>
		/// Performs a refund or a partial refund.
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
		/// <param name="args">
		/// The processor arguments.
		/// </param>
		/// <returns>
		/// The <see cref="IPaymentResult"/>.
		/// </returns>
		protected override IPaymentResult PerformRefundPayment(IInvoice invoice, IPayment payment, decimal amount, ProcessorArgumentCollection args)
		{
			var record = payment.GetPayTraceTransactionRecord();

			if (StringExtensions.IsNullOrWhiteSpace(record.Data.TRANSACTIONID))
			{
				var error = new NullReferenceException("PayTrace transaction could not be found and/or deserialized from payment extended data collection");
				return new PaymentResult(Attempt<IPayment>.Fail(payment, error), invoice, false);
			}

			var attempt = _paytraceService.Refund(invoice, payment, amount);

			// store the transaction
			var refundTransActions = record.RefundTransactions.ToList();
			refundTransActions.Add(attempt);
			record.RefundTransactions = refundTransActions;

			if (!attempt.Success())
			{
				// In the case of a failure, package up the exception so we can bubble it up.
				var ex = new PayTraceRedirectApiException("PayTrace Checkout  refund response ACK was not Success");
				//if (record.SetCheckout.ErrorTypes.Any()) ex.ErrorTypes = record.SetCheckout.ErrorTypes;

				// ensure that transaction is stored in the payment
				payment.SavePayTraceTransactionRecord(record);
				GatewayProviderService.Save(payment);

				return new PaymentResult(Attempt<IPayment>.Fail(payment, ex), invoice, false);
			}

			foreach (var applied in payment.AppliedPayments())
			{
				applied.TransactionType = AppliedPaymentType.Refund;
				applied.Amount = 0;
				applied.Description += " - Refunded";
				this.GatewayProviderService.Save(applied);
			}

			payment.Amount = payment.Amount - amount;

			if (payment.Amount != 0)
			{
				this.GatewayProviderService.ApplyPaymentToInvoice(payment.Key, invoice.Key, AppliedPaymentType.Debit, "To show partial payment remaining after refund", payment.Amount);
			}

			this.GatewayProviderService.Save(payment);

			return new PaymentResult(Attempt<IPayment>.Succeed(payment), invoice, false);
		}

		/// <summary>
		/// Does the actual work of voiding a payment
		/// </summary>
		/// <param name="invoice">The invoice to which the payment is associated</param>
		/// <param name="payment">The payment to be voided</param>
		/// <param name="args">Additional arguments required by the payment processor</param>
		/// <returns>A <see cref="IPaymentResult"/></returns>
		protected override IPaymentResult PerformVoidPayment(IInvoice invoice, IPayment payment, ProcessorArgumentCollection args)
		{
			foreach (var applied in payment.AppliedPayments())
			{
				applied.TransactionType = AppliedPaymentType.Void;
				applied.Amount = 0;
				applied.Description += " - **Void**";
				GatewayProviderService.Save(applied);
			}

			payment.Voided = true;
			GatewayProviderService.Save(payment);

			return new PaymentResult(Attempt<IPayment>.Succeed(payment), invoice, false);
		}

	}
}
