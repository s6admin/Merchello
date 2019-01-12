using System;
using System.Net;
using System.Web.Mvc;

using Merchello.Core;
using Merchello.Core.Events;
using Merchello.Core.Gateways;
using Merchello.Core.Gateways.Payment;
using Merchello.Core.Logging;
using Merchello.Core.Models;
using Merchello.Providers.Models;
using Merchello.Providers.Payment.PayTrace.Models;
using Merchello.Providers.Payment.PayTrace.Provider;
using Merchello.Providers.Payment.PayTrace.Services;
using Merchello.Web.Models.Ui.Async;
using Merchello.Web.Mvc;

using Umbraco.Core.Events;
using Umbraco.Web.Mvc;

using Constants = Merchello.Providers.Constants;

namespace Merchello.Providers.Payment.PayTrace.Controllers
{
	/// <summary>
	/// A surface controller for used for accepting PayTrace Redirect Payments.
	/// </summary>
	[PluginController("Merchello")]
	public class PayTraceRedirectController : PayTraceRedirectSurfaceControllerBase
	{
		private IPayTraceRedirectService _paytraceService;
		private string _successUrl;
		private string _cancelUrl;
		private bool _deleteInvoiceOnCancel;
		private PayTraceRedirectPaymentGatewayMethod _paymentMethod;

		public PayTraceRedirectController()
		{
			this.Initialize();
		}

		/// <summary>
		/// Occurs before redirecting for a successful response.
		/// </summary>
		public static event TypedEventHandler<PayTraceRedirectController, ObjectEventArgs<PaymentRedirectingUrl>> RedirectingForSuccess;

		/// <summary>
		/// Occurs before redirecting for a cancel response.
		/// </summary>
		public static event TypedEventHandler<PayTraceRedirectController, ObjectEventArgs<PaymentRedirectingUrl>> RedirectingForCancel;

		/// <summary>
		/// Occurs after the final redirection and before redirecting to the success URL
		/// </summary>
		/// <remarks>
		/// Can be used to send OrderConfirmation notification
		/// </remarks>
		public static event TypedEventHandler<PayTraceRedirectController, PaymentAttemptEventArgs<IPaymentResult>> Processed;

		/// <summary>
		/// Handles a successful payment response from the PayTrace Redirect transaction
		/// </summary>
		/// <param name="invoiceKey">The invoice key.</param>
		/// <param name="paymentKey">The payment key.</param>
		/// <param name="token">The token.</param>
		/// <param name="payerId">The payer id.</param>
		/// <returns>
		/// The <see cref="ActionResult" />.
		/// </returns>
		public override ActionResult Success(Guid invoiceKey, Guid paymentKey, string token, string payerId)
		{
			var redirecting = new PaymentRedirectingUrl("Success") { RedirectingToUrl = _successUrl };

			var logData = GetExtendedLoggerData();

			try
			{
				var invoice = GetInvoice(invoiceKey);
				var payment = GetPayment(paymentKey);

				// We can now capture the payment

				// The PayPal Express gateway requires a callback but PayTrace may not
				// The response data is helpful so that we can refund the payment later through the back office if needed.
				var attempt = invoice.CapturePayment(payment, _paymentMethod, invoice.Total);

				// Raise the event to process the email
				Processed.RaiseEvent(new PaymentAttemptEventArgs<IPaymentResult>(attempt), this);

				// If this is an AJAX request return the JSON
				//if (payment.ExtendedData.GetPayTraceRequestIsAjaxRequest())
				//{
				//	var resp = new PaymentResultAsyncResponse
				//	{
				//		Success = attempt.Payment.Success,
				//		InvoiceKey = attempt.Invoice.Key,
				//		PaymentKey = attempt.Payment.Result.Key,
				//		PaymentMethodName = "PayTrace Checkout"
				//	};

				//	if (attempt.Payment.Exception != null)
				//		resp.Messages.Add(attempt.Payment.Exception.Message);

				//	return Json(resp);
				//}

				if (attempt.Payment.Success)
				{
					// we need to empty the basket here
					Basket.Empty();

					// raise the event so the redirect URL can be manipulated
					RedirectingForSuccess.RaiseEvent(new ObjectEventArgs<PaymentRedirectingUrl>(redirecting), this);

					return Redirect(redirecting.RedirectingToUrl);
				}

				var retrying = new PaymentRedirectingUrl("Cancel") { RedirectingToUrl = _cancelUrl };
				var qs = string.Format("?invoicekey={0}&paymentkey={1}", invoiceKey, paymentKey);
				if (!retrying.RedirectingToUrl.IsNullOrWhiteSpace()) return Redirect(retrying.RedirectingToUrl + qs);

				var invalidOp = new InvalidOperationException("Retry url was not specified");

				MultiLogHelper.Error<PayTraceRedirectController>("Could not redirect to retry", invalidOp);
				throw invalidOp;
			}
			catch (Exception ex)
			{
				var extra = new { InvoiceKey = invoiceKey, PaymentKey = paymentKey, Token = token, PayerId = payerId };

				logData.SetValue<object>("extra", extra);

				MultiLogHelper.Error<PayTraceRedirectController>(
					"Failed to Capture SUCCESSFUL PayTrace checkout response.",
					ex,
					logData);

				throw;
			}
		}

		/// <summary>
		/// Handles a cancellation response from the PayTrace Redirect transaction
		/// </summary>
		/// <param name="invoiceKey">The invoice key.</param>
		/// <param name="paymentKey">The payment key.</param>
		/// <param name="token">The token.</param>
		/// <param name="payerId">The payer id.</param>
		/// <returns>
		/// The <see cref="ActionResult" />.
		/// </returns>
		/// <exception cref="System.NotImplementedException"></exception>
		public override ActionResult Cancel(Guid invoiceKey, Guid paymentKey, string token, string payerId = null)
		{
			throw new NotImplementedException();
		}

		private void Initialize()
		{
			var provider = GatewayContext.Payment.GetProviderByKey(Constants.PayTrace.GatewayProviderSettingsKey) as PayTraceRedirectPaymentGatewayProvider;
		}
	}
}
