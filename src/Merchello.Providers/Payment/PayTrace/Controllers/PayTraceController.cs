
namespace Merchello.Providers.Payment.PayTrace.Controllers
{

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
	using Newtonsoft.Json;

	/// <summary>
	/// A surface controller for used for accepting PayTrace Payments.
	/// </summary>
	[PluginController("Merchello")]
	public class PayTraceController : PayTraceSurfaceControllerBase
	{

		/// <summary>
		/// The <see cref="IPayTraceApiPaymentService"/>.
		/// </summary>
		private IPayTraceApiService _PayTraceApiService;

		/// <summary>
		/// The URL for a Success return.
		/// </summary>
		private string _successUrl;

		/// <summary>
		/// The URL for a Cancel return.
		/// </summary>
		private string _cancelUrl;

		/// <summary>
		/// A value indicating whether or not to delete the invoice on cancel.
		/// </summary>
		/// <remarks>
		/// If false the authorize payment is voided.
		/// </remarks>
		private bool _deleteInvoiceOnCancel;

		/// <summary>
		/// The <see cref="PayTraceCheckoutPaymentGatewayMethod"/>.
		/// </summary>
		private PayTracePaymentGatewayMethod _paymentMethod;

		/// <summary>
		/// Initializes a new instance of the <see cref="PayTraceController"/> class.
		/// </summary>
		public PayTraceController()
		{
			this.Initialize();
		}

		/// <summary>
		/// Occurs before redirecting for a successful response.
		/// </summary>
		public static event TypedEventHandler<PayTraceController, ObjectEventArgs<PaymentRedirectingUrl>> RedirectingForSuccess;

		/// <summary>
		/// Occurs before redirecting for a cancel response.
		/// </summary>
		public static event TypedEventHandler<PayTraceController, ObjectEventArgs<PaymentRedirectingUrl>> RedirectingForCancel;

		/// <summary>
		/// Occurs after the final redirection and before redirecting to the success URL
		/// </summary>
		/// <remarks>
		/// Can be used to send OrderConfirmation notification
		/// </remarks>
		public static event TypedEventHandler<PayTraceController, PaymentAttemptEventArgs<IPaymentResult>> Processed;
		
		public override ActionResult Success(Guid invoiceKey, Guid paymentKey, string token, string payerId)
		{
			var redirecting = new PaymentRedirectingUrl("Success") { RedirectingToUrl = _successUrl };

			var logData = GetExtendedLoggerData();

			try
			{
				var invoice = GetInvoice(invoiceKey);
				var payment = GetPayment(paymentKey);

				// We can now capture the payment
				// This will actually make a few more API calls back to PayTrace to get required transaction
				// data so that we can refund the payment later through the back office if needed.
				var attempt = invoice.CapturePayment(payment, _paymentMethod, invoice.Total);

				// Raise the event to process the email
				Processed.RaiseEvent(new PaymentAttemptEventArgs<IPaymentResult>(attempt), this);

				// If this is an AJAX request return the JSON
				if (payment.ExtendedData.GetPayTraceRequestIsAjaxRequest())
				{
					var resp = new PaymentResultAsyncResponse
					{
						Success = attempt.Payment.Success,
						InvoiceKey = attempt.Invoice.Key,
						PaymentKey = attempt.Payment.Result.Key,
						PaymentMethodName = "PayTrace Checkout"
					};

					if (attempt.Payment.Exception != null)
						resp.Messages.Add(attempt.Payment.Exception.Message);

					return Json(resp);
				}

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

				MultiLogHelper.Error<PayTraceController>("Could not redirect to retry", invalidOp);
				throw invalidOp;
			}
			catch (Exception ex)
			{
				var extra = new { InvoiceKey = invoiceKey, PaymentKey = paymentKey, Token = token, PayerId = payerId };

				logData.SetValue<object>("extra", extra);

				MultiLogHelper.Error<PayTraceController>(
					"Failed to Capture SUCCESSFUL PayTrace checkout response.",
					ex,
					logData);

				throw;
			}
		}

		public override ActionResult Cancel(Guid invoiceKey, Guid paymentKey, string token, string payerId = null)
		{
			var redirecting = new PaymentRedirectingUrl("Cancel") { RedirectingToUrl = _cancelUrl };

			try
			{
				var invoice = GetInvoice(invoiceKey);
				var payment = GetPayment(paymentKey);

				if (_deleteInvoiceOnCancel)
				{
					InvoiceService.Delete(invoice);
				}
				else
				{
					payment.VoidPayment(invoice, _paymentMethod.PaymentMethod.Key);
				}

				// raise the event so the redirect URL can be manipulated
				RedirectingForCancel.RaiseEvent(new ObjectEventArgs<PaymentRedirectingUrl>(redirecting), this);
				return Redirect(redirecting.RedirectingToUrl);
			}
			catch (Exception ex)
			{
				var logData = GetExtendedLoggerData();

				var extra = new { InvoiceKey = invoiceKey, PaymentKey = paymentKey, Token = token, PayerId = payerId };

				logData.SetValue<object>("extra", extra);

				MultiLogHelper.Error<PayTraceController>(
					"Failed to Cancel PayTrace  checkout response.",
					ex,
					logData);

				throw;
			}
		}

		/// <summary>
		/// Initializes the controller.
		/// </summary>
		private void Initialize()
		{

			var provider = GatewayContext.Payment.GetProviderByKey(Constants.PayTrace.GatewayProviderSettingsKey) as PayTracePaymentGatewayProvider;
			if (provider == null)
			{
				var nullRef =
					new NullReferenceException(
						"PayTracePaymentGatewayProvider is not activated or has not been resolved.");
				MultiLogHelper.Error<PayTraceController>(
					"Failed to find active PayTracePaymentGatewayProvider.",
					nullRef,
					GetExtendedLoggerData());

				throw nullRef;
			}

			#region S6 Extracted from Merchello.Providers.Models.ProviderSettingsExtensions so we don't have to alter a shared core file

			PayTraceProviderSettings settings = null;
			if (provider.ExtendedData.ContainsKey(Constants.PayTrace.ExtendedDataKeys.ProviderSettings))
			{
				var json = provider.ExtendedData.GetValue(Constants.PayTrace.ExtendedDataKeys.ProviderSettings);
				settings = JsonConvert.DeserializeObject<PayTraceProviderSettings>(json);
			}
			else
			{
				settings = new PayTraceProviderSettings();
			}

			#endregion

			// instantiate the service
			_PayTraceApiService = new PayTraceApiService(settings);
						
			_successUrl = settings.SuccessUrl;
			_cancelUrl = settings.CancelUrl;
			_deleteInvoiceOnCancel = settings.DeleteInvoiceOnCancel;

			_paymentMethod = provider.GetPaymentGatewayMethodByPaymentCode(Constants.PayTrace.PaymentCodes.Checkout) as PayTracePaymentGatewayMethod;

			if (_paymentMethod == null)
			{
				var nullRef = new NullReferenceException("PayTraceCheckoutPaymentGatewayMethod could not be instantiated");
				MultiLogHelper.Error<PayTraceController>("PayTraceCheckoutPaymentGatewayMethod was null", nullRef, GetExtendedLoggerData());

				throw nullRef;
			}
		}

	}
}
