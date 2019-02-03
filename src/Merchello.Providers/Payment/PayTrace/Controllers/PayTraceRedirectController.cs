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
using Merchello.Core.Services;
using Merchello.Core.Checkout;
using Merchello.Web;
using Merchello.Web.Models.SaleHistory;

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

		// S6 So OnFinalizing can be broadcast after a redirect payment has been successfully detected
		public static event TypedEventHandler<CheckoutPaymentManagerBase, CheckoutEventArgs<IPaymentResult>> Finalizing;

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

			Console.WriteLine(Basket);

			// Reset the Customer's Basket and CheckoutManager data
			Basket.Empty();
			var checkoutManager = Basket.GetCheckoutManager();
			checkoutManager.Customer.Reset();
			checkoutManager.Offer.Reset();
			checkoutManager.Extended.Reset();
			checkoutManager.Payment.Reset();
			checkoutManager.Shipping.Reset();

			try
			{
				/* 
					REMOVED: Client has determined AuthKey is not necessary so we only need to handle the Success redirect
					Get payment and add AuthKey (Token) to its ExtendedData, this can only be done here because it is the only place the AuthKey parameter is returned from PayTrace
				*/
				//var invoice = GetInvoiceByOrderId(r.OrderId);
				//var payment = invoice.Payments().FirstOrDefault(x => x.Amount == invoice.Total && x.Authorized);
				//var record = payment.GetPayTraceTransactionRecord();
				//record.Data.AUTHKEY = r.Token; // PayTrace AuthKey, which is only passed to success, not the silent response

				//payment.SavePayTraceTransactionRecord(record);
				
			} catch(Exception ex)
			{
				MultiLogHelper.Error<PayTraceRedirectController>(ex.Message, ex);
			}
			
			var redirecting = new PaymentRedirectingUrl("Success") { RedirectingToUrl = _successUrl };

			// TODO Confirm success and redirect

			return Redirect(redirecting.RedirectingToUrl); // Temp for testing to stop capturepayment attempts...Below is all moving to silent response handler method

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
			// NOTE: Customer/Basket contexts are out of scope here so any actions related to checkout don't do anything. Use Success() instead for tasks like reseting CheckoutManager and the basket.
			// Invoice and/or database tasks should still be performed

			PayTraceRedirectSilentResponse r = PayTraceHelper.ParsePayTraceSilentParamList(parmList);

			/* 
				NOTE: PayTrace example docs are missing some properties. The full param list returned includes:

				ORDERID, TRANSACTIONID,	APPMSG, AVSRESPONSE, CSCRESPONSE, EMAIL, BNAME, CARDTYPE, EXPMNTH, EXPYR, LAST4, AMOUNT
				
				PayTrace Doc Example:
				parmList=ORDERID%7E123456%7CTRANSACTIONID%7E62279788%7CAPPCODE%7ETAS456%7CAPPMSG%7E++NO++MATCH++++++%2D+Approved+and+completed%7CAVSRESPONSE%7ENo+Match%7CCSCRESPONSE%7EMatch%7CEMAIL%7Etest%40test%2Ecom%7C			
			*/
						
			try
			{
				
				var invoice = GetInvoiceByOrderId(r.OrderId); 
				
				var payments = invoice.Payments();
				if(payments == null || !payments.Any())
				{
					// No payments found for processed Invoice, log
					Exception ex = new Exception("No payments found for Invoice " + r.OrderId + " in PayTrace silent response.");
					MultiLogHelper.Error(typeof(PayTraceRedirectController), ex.Message, ex);
					// TODO Business Rules - How to handle remaining checkout?
					return;
				}
								
				var payment = payments.FirstOrDefault(x => x.Amount == invoice.Total && !x.Authorized);

				if(payment == null)
				{
					Exception ex = new Exception("Could not find payment for Invoice " + invoice.Key + " during PayTrace silent response.");
					MultiLogHelper.Error<PayTraceRedirectController>(ex.Message, ex);
					// TODO Business Rules - How to handle remaining checkout?
					return;	
				}
								
				// Add returned values to the existing payment extendedData record				
				PayTraceRedirectTransactionRecord record = payment.GetPayTraceTransactionRecord();
				record.Data.ORDERID = r.OrderId;
				record.Data.APPCODE = r.AppCode;
				record.Data.APPMSG = r.AppMsg;
				record.Data.AVSRESPONSE = r.AvsResponse;
				record.Data.CSCRESPONSE = r.CscResponse;
				record.Data.EMAIL = r.Email;
				record.Data.RESPONSEMESSAGE = r.ResponseMessage;
				record.Data.Authorized = true;
				record.Data.CARDTYPE = r.CardType;
				record.Data.EXPMNTH = r.CardExpireMonth;
				record.Data.EXPYR = r.CardExpireYear;
				record.Data.LAST4 = r.CardLastFour;
				record.Data.BNAME = r.BillingName;
				record.Data.TRANSACTIONID = r.TransactionId;				
				//promiseRecord.Data.Token = PayTrace AuthKey is not available in this call so it is saved in the Success handler

				payment.SavePayTraceTransactionRecord(record); // Save data changes to ExtendedData

				// We can now capture the payment								
				// The response data is helpful so that we can refund the payment later through the back office if needed.
				var captureAttempt = invoice.CapturePayment(payment, _paymentMethod, invoice.Total);

				// Raise the event to process the email
				Processed.RaiseEvent(new PaymentAttemptEventArgs<IPaymentResult>(captureAttempt), this);

				if (captureAttempt.Payment.Success)
				{

					//Basket.Empty();

					#region S6 OnFinalizing()

					// Faux OnFinalizing() to emulate Merchello.Web/UmbracoApplicationEventHandler.cs#L439 because our custom AuthorizePayment method prevented the default eComm event from firing when the customer initiated a redirect payment

					captureAttempt.Invoice.AuditCreated();

					if (captureAttempt.Payment.Success)
					{
						// Reset the Customer's CheckoutManager
						//var checkoutManager = Basket.GetCheckoutManager();
						//checkoutManager.Customer.Reset();
						//checkoutManager.Offer.Reset();
						//checkoutManager.Extended.Reset();
						//checkoutManager.Payment.Reset();
						//checkoutManager.Shipping.Reset();

						if (captureAttempt.Invoice.InvoiceStatusKey == Core.Constants.DefaultKeys.InvoiceStatus.Paid)
						{
							captureAttempt.Payment.Result.AuditPaymentCaptured(captureAttempt.Payment.Result.Amount);
						}
						else
						{
							captureAttempt.Payment.Result.AuditPaymentAuthorize(captureAttempt.Invoice);
						}
					}
					else
					{
						captureAttempt.Payment.Result.AuditPaymentDeclined();
					}

					#endregion S6 OnFinalizing()

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
