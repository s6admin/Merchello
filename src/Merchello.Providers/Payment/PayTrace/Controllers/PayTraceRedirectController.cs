using System;
using System.Net;
using System.Web.Mvc;

using Merchello.Core;
using Merchello.Core.Events;
using Merchello.Core.Gateways;
using Merchello.Core.Gateways.Payment;
using Merchello.Core.Logging;
using Merchello.Core.Models;
using MC = Merchello.Providers.Constants;
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
using Umbraco.Core.Logging;
using Merchello.Core.Configuration;
using Merchello.Web.Pluggable;

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
		//private string _cancelUrl; // Removed PayTrace does not have a "cancel" concept
		private string _declinedUrl;
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

		// S6: So OnFinalizing can be broadcast after a redirect payment has been successfully detected
		//public static event TypedEventHandler<CheckoutPaymentManagerBase, CheckoutEventArgs<IPaymentResult>> Finalizing;
		public static event TypedEventHandler<PayTraceRedirectController, CheckoutEventArgs<IPaymentResult>> Finalizing;

		/// <summary>
		/// Occurs after the final redirection and before redirecting to the success URL
		/// </summary>
		/// <remarks>		
		/// </remarks>
		public static event TypedEventHandler<PayTraceRedirectController, PaymentAttemptEventArgs<PayTraceRedirectSilentResponse>> Processed;

		/// <summary>
		/// S6 An event broadcast for external use when the PayTrace Success method hook has been reached.
		/// </summary>
		public static event TypedEventHandler<PayTraceRedirectController, PaymentAttemptEventArgs<PayTraceRedirectResponse>> OnSuccess;

		/// <summary>
		/// S6 An event broadcast for external use when the PayTrace Decline method hook has been reached.
		/// </summary>
		public static event TypedEventHandler<PayTraceRedirectController, PaymentAttemptEventArgs<PayTraceRedirectResponse>> OnDeclined;

		/// <summary>
		/// Handles a successful payment response from the PayTrace Redirect transaction
		/// </summary>		
		/// <returns>
		/// The <see cref="ActionResult" />.
		/// </returns>		
		public override ActionResult Success(string parmList)
		{

			LogHelper.Warn(typeof(PayTraceRedirectController), "PayTraceSuccess received. ParamList: " + parmList);

			PayTraceRedirectResponse r = null;

			try
			{
				r =	PayTraceHelper.ParsePayTraceParamList(parmList);
			} catch(Exception ex)
			{
				LogHelper.Error(typeof(PayTraceRedirectController), "Error while parsing PayTrace Success response. ", ex);
			}
			
			var invoice = GetInvoiceByOrderId(r.OrderId);
			
			OnSuccess.RaiseEvent(new PaymentAttemptEventArgs<PayTraceRedirectResponse>(r), this);			
			
			var cm = CustomerContext.CurrentCustomer.Basket().GetCheckoutManager(MerchelloConfiguration.Current.CheckoutContextSettings);
            Console.WriteLine(cm);
			
			Basket.Empty();
			
			ResetAllCheckoutData();

			// Ensure invoiceKey is present in the CustomerContext in case resetting the Customer or CheckoutManager causes it to get lost
			if (invoice != null)
			{
				try
				{

					if (Merchello.Core.StringExtensions.IsNullOrWhiteSpace(CustomerContext.GetValue("invoiceKey")))
					{
						//LogHelper.Warn(typeof(PayTraceRedirectController), "CustomerContext InvoiceKey is NULL. Attempting to set from invoice: " + invoice.Key);
						CustomerContext.SetValue("invoiceKey", invoice.Key.ToString()); // Re-apply invoiceKey after clearing CheckoutManager so SalesReceipt page still shows the appropriate invoice
						LogHelper.Warn(typeof(PayTraceRedirectController), "CustomerContext InvoiceKey set: " + invoice.Key);
					}
					else
					{
						LogHelper.Warn(typeof(PayTraceRedirectController), "CustomerContext InvoiceKey retained: " + invoice.Key);
					}

				}
				catch (Exception ex)
				{
					LogHelper.Error(typeof(PayTraceRedirectController), "Error setting Customer Context Invoice Key. ", ex);
				}
			}

			var redirecting = new PaymentRedirectingUrl("Success") { RedirectingToUrl = _successUrl };
			
			return Redirect(redirecting.RedirectingToUrl); 
		}

		/// <summary>
		/// Handles a Declined response from the payment provider. Declined orders are still accepted but will display a warning message to the customer on the receipt page that their payment was not processed.
		/// </summary>
		/// <param name="parmList">The parm list.</param>
		/// <returns></returns>
		public override ActionResult Declined(string parmList)
		{
			/*
			/declined?parmList=OrderID~2019020504440768%7CAuthKey~7487400%7CEMAIL~test%40test%2Etest%7C
			*/
			LogHelper.Warn(typeof(PayTraceRedirectController), "PayTraceDeclined received. ParamList: " + parmList);

			PayTraceRedirectResponse r = null;

			try
			{
				r = PayTraceHelper.ParsePayTraceParamList(parmList);
			} catch(Exception ex)
			{
				LogHelper.Error(typeof(PayTraceRedirectController), "Error while parsing PayTrace declined response. ", ex);
			}
			
			
			// _declinedUrl is available, but _successUrl is set to Receipt page by default which will still be shown to customers after a decline but with additional details about their unpaid order
			var redirecting = new PaymentRedirectingUrl("Declined") { RedirectingToUrl = _successUrl };
			var invoice = GetInvoiceByOrderId(r.OrderId);

			OnDeclined.RaiseEvent(new PaymentAttemptEventArgs<PayTraceRedirectResponse>(r), this);						

			try
			{
				
				var payment = invoice.Payments().FirstOrDefault();
				Guid methodKey = payment.PaymentMethodKey ?? Guid.Empty; // VoidPayment() requires a non-nullable Guid
				if (!methodKey.Equals(Guid.Empty))
				{
					//payment.VoidPayment(invoice, methodKey);
				}				
			}
			catch (Exception ex)
			{
				LogHelper.Error(typeof(PayTraceRedirectController), ex.Message, ex);
			}

			/* 
				Keep track of the failed attempts in the Customer data so the next checkout step is aware of the payment failure(s)			
				For v1 the Invoice UNPAID Status is used to indicate if a message should be displayed on the SalesReceipt page but
				tracking failed attempts will be helpful once customers are permitted to "retry" their payments in v1.5
			*/
			//int attempts = 1;
			//ExtendedDataCollection ed = CurrentCustomer.ExtendedData;
			//var c1 = CustomerContext.CurrentCustomer; // CurrentCustomer

			//// Retrieve previous saved value if it exists
			//if (ed.ContainsKey(MC.PayTraceRedirect.ExtendedDataKeys.FailedAttempts))
			//{
			//	int.TryParse(ed.GetValue(MC.PayTraceRedirect.ExtendedDataKeys.FailedAttempts), out attempts); // Retrieve previous value
			//	attempts = attempts + 1; // Increment previous value				
			//}

			//ed.SetValue(MC.PayTraceRedirect.ExtendedDataKeys.FailedAttempts, attempts.ToString());
			//ViewData[MC.PayTraceRedirect.ExtendedDataKeys.FailedAttempts] = attempts;

			// https://our.umbraco.com/packages/collaboration/merchello/merchello/85200-invoice-items-missing-in-second-order

			//LogHelper.Warn(typeof(PayTraceRedirectController), "CustomerContext InvoiceKey is: " + invoice.Key);

			Console.WriteLine(CurrentCustomer);
			//var cm = Basket.GetCheckoutManager(MerchelloConfiguration.Current.CheckoutContextSettings);
			//Console.WriteLine(cm);

			Basket.Empty();
			/*
				WARNING
				Reset is called in SilentResponse regardless of success or decline payment result. 
				Do NOT call reset again here as it causes unpredictable results to the customer context and receipt page!
			*/
			ResetAllCheckoutData();
			
			// Ensure invoiceKey is present in the CustomerContext in case resetting the Customer or CheckoutManager causes it to get lost
			if(invoice != null)
			{
				try
				{
					if (invoice != null && Merchello.Core.StringExtensions.IsNullOrWhiteSpace(CustomerContext.GetValue("invoiceKey")))
					{
						LogHelper.Warn(typeof(PayTraceRedirectController), "CustomerContext InvoiceKey is NULL. Attempting to set from invoice: " + invoice.Key);
						CustomerContext.SetValue("invoiceKey", invoice.Key.ToString()); // Re-apply invoiceKey after clearing CheckoutManager so SalesReceipt page still shows the appropriate invoice
						LogHelper.Warn(typeof(PayTraceRedirectController), "CustomerContext InvoiceKey is now: " + invoice.Key);
					}
					else
					{
						LogHelper.Warn(typeof(PayTraceRedirectController), "CustomerContext InvoiceKey retained: " + invoice.Key);
					}
				}
				catch (Exception ex)
				{
					LogHelper.Error(typeof(PayTraceRedirectController), "Error setting Customer Context Invoice Key. ", ex);
				}
			}			
			
			return Redirect(redirecting.RedirectingToUrl);
		}

		// The Url hooked by the PayTrace Redirect silent response (in ALL success or fail cases). This is not a conventional Payment Provider workflow so try to avoid doing anything eComm-related (Customer, Basket, etc...) other than capturing a payment
		public void PayTraceSilentResponse(string parmList)
		{
			// NOTE: Customer/Basket contexts are out of scope here so any actions related to checkout don't do anything. Use Success() instead for tasks like resetting CheckoutManager and the basket.
			// Invoice and/or database tasks should still be performed

			LogHelper.Warn(typeof(PayTraceRedirectController), "PayTraceSilentResponse received. ParamList: " + parmList);

			PayTraceRedirectSilentResponse r = null;

			try
			{
				r = PayTraceHelper.ParsePayTraceSilentParamList(parmList);
			}
			catch(Exception ex)
			{
				LogHelper.Error(typeof(PayTraceRedirectController), "Error while parsing PayTrace silent response. ", ex);
			}
			
			if(r == null)
			{
				Exception ex = new Exception("Could not parse PayTraceSilentResponse data. Response data is null! Nothing to process.");
				LogHelper.Error(typeof(PayTraceRedirectController), ex.Message, ex);
				return;
			}

			/* 
				NOTE: PayTrace example docs are missing some properties. The full param list returned includes:

				ORDERID, TRANSACTIONID,	APPMSG, AVSRESPONSE, CSCRESPONSE, EMAIL, BNAME, CARDTYPE, EXPMNTH, EXPYR, LAST4, AMOUNT
				
				PayTrace Doc Example:
				parmList=ORDERID%7E123456%7CTRANSACTIONID%7E62279788%7CAPPCODE%7ETAS456%7CAPPMSG%7E++NO++MATCH++++++%2D+Approved+and+completed%7CAVSRESPONSE%7ENo+Match%7CCSCRESPONSE%7EMatch%7CEMAIL%7Etest%40test%2Ecom%7C			
			*/

			try
			{

				// The Processed handler triggers each time a response is received from the payment provider, regardless of if a capture payment is attempted or not
				Processed.RaiseEvent(new PaymentAttemptEventArgs<PayTraceRedirectSilentResponse>(r), this);
								
				// Record all PayTrace response values regardless of payment success/failure
				var invoice = GetInvoiceByOrderId(r.OrderId); 
				
				if(invoice == null)
				{
					LogHelper.Warn(typeof(PayTraceRedirectController), "Could not find matching invoice for PayTrace Silent Response " + r.OrderId + ". Attempt to capture payment skipped.");					
					return;
				}

				var payments = invoice.Payments();
				if(payments == null || !payments.Any())
				{
					// No payments found for processed Invoice, log
					Exception ex = new Exception("No payments found for Invoice " + r.OrderId + " in PayTrace silent response.");
					MultiLogHelper.Error(typeof(PayTraceRedirectController), ex.Message, ex);					
					return;
				}
				
				// NOTE For V1 client has confirmed no partial payments are necessary but that may change in the future so using the silent response AMOUNT (not just the invoice.Total) will be important
								
				// Retrieve initial promise payment
				var payment = payments.FirstOrDefault(x => x.Amount == invoice.Total && !x.Authorized);

				if(payment == null)
				{
					Exception ex = new Exception("Could not find payment for Invoice " + invoice.Key + " during PayTrace silent response.");
					MultiLogHelper.Error<PayTraceRedirectController>(ex.Message, ex);					
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
				record.Data.AMOUNT = r.Amount;					
				//promiseRecord.Data.Token = PayTrace AuthKey is not available in this call so it is saved in the Success handler

				payment.SavePayTraceTransactionRecord(record); // Save data changes to ExtendedData regardless of payment success/failure
				
				if (r.Success)
				{
					// We can now capture the payment													
					var captureAttempt = invoice.CapturePayment(payment, _paymentMethod, invoice.Total);
										
					if (captureAttempt.Payment.Success)
					{

						#region S6 OnFinalizing()
																		
						// Hooks UmbracoApplicationEventHandler.cs (eComm core) and custom PayTraceRedirectEventHandler.cs (client project)
						Finalizing.RaiseEvent(new CheckoutEventArgs<IPaymentResult>(CurrentCustomer, captureAttempt), this);
						
						// Faux OnFinalizing() to emulate Merchello.Web/UmbracoApplicationEventHandler.cs#L439 because our custom AuthorizePayment method prevented the default eComm event from firing when the customer initiated a redirect payment
						// https://our.umbraco.com/packages/collaboration/merchello/merchello/81312-retain-shipping-address-after-redirect
						
						#endregion S6 OnFinalizing()
					}
				} else
				{
					// PayTrace returned a failure. The record details have been saved to the main Payment data but don't create an Applied Payment
					LogHelper.Warn(typeof(PayTraceRedirectController), "PayTrace Redirect payment for order " + r.OrderId + " was flagged as unsuccessful. Please review merchant logs for additional details.");

					/*
					Basic retry logic from PayPalExpressController. May not be usable in current form with PayTraceRedirect provider
					 var retrying = new PaymentRedirectingUrl("Cancel") { RedirectingToUrl = _cancelUrl };
					var qs = string.Format("?invoicekey={0}&paymentkey={1}", invoiceKey, paymentKey);
					if (!retrying.RedirectingToUrl.IsNullOrWhiteSpace()) return Redirect(retrying.RedirectingToUrl + qs);

					var invalidOp = new InvalidOperationException("Retry url was not specified");

					MultiLogHelper.Error<PayPalExpressController>("Could not redirect to retry", invalidOp);
					throw invalidOp;
					*/
				}
			}
			catch (Exception ex)
			{
				MultiLogHelper.Error<PayTraceRedirectController>(
					"Error encountered while processing PayTrace Redirect payment in silent response. ",
					ex);				
			}
		}

		private IInvoice GetInvoiceByOrderId(string id)
		{
			if (id == null || id.Length == 0) return null;

			// Retrieve subset of Invoices within the past 48 hours to limit the cost of this db call. If a matching invoice isn't found, then search within all invoices
			//IInvoice invoice = InvoiceService.GetInvoicesByDateRange((DateTime)SqlDateTime.MinValue, (DateTime)SqlDateTime.MaxValue).OrderBy(x => x.InvoiceNumber).FirstOrDefault(x => x.PoNumber == id);			
			IInvoice invoice = InvoiceService.GetInvoicesByDateRange(DateTime.Now.Subtract(new TimeSpan(2,0,0)), DateTime.Now.Add(new TimeSpan(1,0,0))).OrderBy(x => x.InvoiceNumber).FirstOrDefault(x => x.PoNumber == id);

			return invoice;
		}

		private bool ResetAllCheckoutData()
		{
			try
			{
				var customerContext = PluggableObjectHelper.GetInstance<CustomerContextBase>("CustomerContext", UmbracoContext);				
				var checkoutManager = customerContext.CurrentCustomer.Basket().GetCheckoutManager(MerchelloConfiguration.Current.CheckoutContextSettings);
				var cm = Basket.GetCheckoutManager(MerchelloConfiguration.Current.CheckoutContextSettings);
				bool matchingManagers = checkoutManager == cm;

				// Leave checkout manager alone
				//checkoutManager.Customer.Reset();
				//checkoutManager.Offer.Reset();
				//checkoutManager.Extended.Reset();
				//checkoutManager.Payment.Reset();
				//checkoutManager.Shipping.Reset();

				// Clear all extendedData including client-specific keys
				foreach(var key in CurrentCustomer.ExtendedData.Keys)
				{
					CurrentCustomer.ExtendedData.RemoveValue(key);
				}
			}
			catch (Exception ex)
			{
				LogHelper.Error(typeof(PayTraceRedirectController), "Error encountered while resetting checkout data. ", ex);
				return false;
			}

			return true;

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
			var settings = provider.ExtendedData.GetPayTraceRedirectProviderSettings();
			_paytraceService = new PayTraceRedirectService(settings);			
			_successUrl = settings.EndUrl; 
			//_cancelUrl = settings.CancelUrl;
			_declinedUrl = settings.DeclinedUrl;
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
