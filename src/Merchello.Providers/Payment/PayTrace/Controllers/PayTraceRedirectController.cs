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
using System.Data.SqlTypes;
using System.Linq;
using Umbraco.Core;

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
		/// <returns>
		/// The <see cref="ActionResult" />.
		/// </returns>		
		public override ActionResult Success(string parmList)
		{
			
			PayTraceRedirectResponse r = PayTraceHelper.ParsePayTraceParamList(parmList);
			var redirecting = new PaymentRedirectingUrl("Success") { RedirectingToUrl = _successUrl };

			return Redirect(redirecting.RedirectingToUrl); // Temp for testing to stop capturepayment attempts...Below is all moving to silent response handler method

			//var logData = GetExtendedLoggerData();
						
			//try
			//{
			//	// Get Invoice from TransactionId which is currently set to the SMO-designed PO Number
			//	var invoice = GetInvoiceByOrderId(r.OrderId); //GetInvoice(invoiceKey);
			//	var payment = GetPayment(Guid.Empty);
				
			//	// We can now capture the payment

			//	// The PayTrace  gateway requires a callback but PayTrace may not
			//	// The response data is helpful so that we can refund the payment later through the back office if needed.
			//	var attempt = invoice.CapturePayment(payment, _paymentMethod, invoice.Total);

			//	// Raise the event to process the email
			//	Processed.RaiseEvent(new PaymentAttemptEventArgs<IPaymentResult>(attempt), this);

			//	// If this is an AJAX request return the JSON
			//	//if (payment.ExtendedData.GetPayTraceRequestIsAjaxRequest())
			//	//{
			//	//	var resp = new PaymentResultAsyncResponse
			//	//	{
			//	//		Success = attempt.Payment.Success,
			//	//		InvoiceKey = attempt.Invoice.Key,
			//	//		PaymentKey = attempt.Payment.Result.Key,
			//	//		PaymentMethodName = "PayTrace Checkout"
			//	//	};

			//	//	if (attempt.Payment.Exception != null)
			//	//		resp.Messages.Add(attempt.Payment.Exception.Message);

			//	//	return Json(resp);
			//	//}

			//	if (attempt.Payment.Success)
			//	{
			//		// we need to empty the basket here
			//		Basket.Empty();

			//		// raise the event so the redirect URL can be manipulated
			//		RedirectingForSuccess.RaiseEvent(new ObjectEventArgs<PaymentRedirectingUrl>(redirecting), this);

			//		return Redirect(redirecting.RedirectingToUrl);
			//	}
				
			//	// TEMP
			//	Guid invoiceKey = Guid.Empty;
			//	Guid paymentKey = Guid.Empty;

			//	var retrying = new PaymentRedirectingUrl("Cancel") { RedirectingToUrl = _cancelUrl };
			//	var qs = string.Format("?invoicekey={0}&paymentkey={1}", invoiceKey, paymentKey);				
			//	if(Merchello.Core.StringExtensions.IsNullOrWhiteSpace(retrying.RedirectingToUrl)) return Redirect(retrying.RedirectingToUrl + qs);
			//	var invalidOp = new InvalidOperationException("Retry url was not specified");

			//	MultiLogHelper.Error<PayTraceRedirectController>("Could not redirect to retry", invalidOp);
			//	throw invalidOp;
			//}
			//catch (Exception ex)
			//{
			//	//var extra = new { InvoiceKey = invoiceKey, PaymentKey = paymentKey, Token = token, PayerId = payerId };

			//	//logData.SetValue<object>("extra", extra);

			//	MultiLogHelper.Error<PayTraceRedirectController>(
			//		"Failed to Capture SUCCESSFUL PayTrace checkout response.",
			//		ex); /*, logData*/

			//	throw;
			//}
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

		// The Url hooked by the PayTrace Redirect silent response
		public void PayTraceSilentResponse(string parmList)
		{

			PayTraceRedirectSilentResponse r = PayTraceHelper.ParsePayTraceSilentParamList(parmList);

			/* 
				ORDERID
				TRANSACTIONID
				APPMSG
				AVSRESPONSE
				CSCRESPONSE
				EMAIL
				----
				Optional: CANAME, EXPRMONTH, EXPRYEAR

				Example:
				parmList=ORDERID%7E123456%7CTRANSACTIONID%7E62279788%7CAPPCODE%7ETAS456%7CAPPMSG%7E++NO++MATCH++++++%2D+Approved+and+completed%7CAVSRESPONSE%7ENo+Match%7CCSCRESPONSE%7EMatch%7CEMAIL%7Etest%40test%2Ecom%7C
			
			*/

			Console.WriteLine("SILENCE!");

			try
			{
				
				var invoice = GetInvoiceByOrderId(r.OrderId); 
				var payments = invoice.Payments();
				if(payments == null || !payments.Any())
				{
					// No payments found for processed Invoice, log
					Exception ex = new Exception("No payments found for Invoice " + r.OrderId + " in PayTrace silent response.");
					MultiLogHelper.Error(typeof(PayTraceRedirectController), ex.Message, ex);
					return;
				}
				
				/* 
					TODO 
					Schema issue: We can't match an IPayment to a TransactionId b/c the promise Payment is made before the PayTrace Redirect occurs 
					so this is the first time the website sees a TransactionId.
				*/

				var payment = payments.FirstOrDefault(); // GetPayment(r.TransactionId);

				// We can now capture the payment								
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
				}												
			}
			catch (Exception ex)
			{
				MultiLogHelper.Error<PayTraceRedirectController>(
					"Failed to Capture SUCCESSFUL PayTrace Redirect payment in silent response.",
					ex); /*, logData*/

				throw;
			}
		}

		private IInvoice GetInvoiceByOrderId(string id)
		{
			if (id == null || id.Length == 0) return null;

			IInvoice invoice = InvoiceService.GetInvoicesByDateRange((DateTime)SqlDateTime.MinValue, (DateTime)SqlDateTime.MaxValue).OrderBy(x => x.InvoiceNumber).FirstOrDefault(x => x.PoNumber == id);			
			
			return invoice;
		}

		private void Initialize()
		{
			var provider = GatewayContext.Payment.GetProviderByKey(Constants.PayTraceRedirect.GatewayProviderSettingsKey) as PayTraceRedirectPaymentGatewayProvider;

			if (provider == null)
			{
				var nullRef =
					new NullReferenceException(
						"PayTracePaymentGatewayProvider is not activated or has not been resolved.");
				MultiLogHelper.Error<PayTraceRedirectController>(
					"Failed to find active PayTracePaymentGatewayProvider.",
					nullRef,
					GetExtendedLoggerData());

				throw nullRef;
			}

			// instantiate the service
			_paytraceService = new PayTraceRedirectService(provider.ExtendedData.GetPayTraceRedirectProviderSettings());

			var settings = provider.ExtendedData.GetPayTraceRedirectProviderSettings();
			_successUrl = settings.EndUrl; // "Success" might mean the silent response in PayTrace as opposed to PayPal //settings.SuccessUrl;
			_cancelUrl = settings.CancelUrl;
			_deleteInvoiceOnCancel = settings.DeleteInvoiceOnCancel;

			_paymentMethod = provider.GetPaymentGatewayMethodByPaymentCode(Constants.PayTraceRedirect.PaymentCodes.RedirectCheckout) as PayTraceRedirectPaymentGatewayMethod;

			if (_paymentMethod == null)
			{
				var nullRef = new NullReferenceException("PayTraceRedirectPaymentGatewayMethod could not be instantiated");
				MultiLogHelper.Error<PayTraceRedirectController>("PayTraceRedirectPaymentGatewayMethod was null", nullRef, GetExtendedLoggerData());

				throw nullRef;
			}
		}		
	}
}
