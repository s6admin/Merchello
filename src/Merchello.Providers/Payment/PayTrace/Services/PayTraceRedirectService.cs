﻿
using System;
using System.Web;
using Merchello.Core;
using Merchello.Core.Models;
using Merchello.Core.Logging;
using Merchello.Providers.Payment.PayTrace.Models;
using Umbraco.Core.Events;
using Merchello.Core.Events;
using Umbraco.Core.Logging;

namespace Merchello.Providers.Payment.PayTrace.Services
{
	public class PayTraceRedirectService : PayTraceAPIServiceBase, IPayTraceRedirectService
	{		
		// Actual methods are defined in PayTraceRedirectAPIController.cs
		private const string DefaultReturnUrl = "{0}/umbraco/merchello/paytraceredirect/success?invoiceKey={1}&paymentKey={2}";

		private const string DefaultCancelUrl = "{0}/umbraco/merchello/paytraceredirect/cancel?invoiceKey={1}&paymentKey={2}";

		private const string DefaultSilentResponse = "{0}/umbraco/merchello/paytraceredirect/paytracesilentresponse?invoiceKey={1}&paymentKey={2}";

		private const string Version = "";

		private readonly string _websiteUrl;

		//private readonly PayTraceRedirectResponseFactory _responseFactory;

		public PayTraceRedirectService(PayTraceRedirectProviderSettings settings) 
			: base(settings)
		{
			_websiteUrl = PayTraceHelper.GetBaseWebsiteUrl();
		}

		/// <summary>
		/// Performs the setup for a checkout.
		/// </summary>
		/// <param name="invoice">The <see cref="IInvoice" />.</param>
		/// <param name="payment">The <see cref="IPayment" /></param>
		/// <returns>
		/// The <see cref="CheckoutResponse" />.
		/// </returns>
		public PayTraceRedirectTransactionRecord SetCheckout(IInvoice invoice, IPayment payment)
		{
			string returnUrl = DefaultReturnUrl;
			string cancelUrl = DefaultCancelUrl;
			return SetCheckout(invoice, payment, returnUrl, cancelUrl);
		}
				
		// This is a capture for PayPal, so far unnecessary logic for PayTrace Redirect
		public PayTraceRedirectTransactionRecord Capture(IInvoice invoice, IPayment payment, decimal amount, bool isPartialPayment)
		{
			var record = payment.GetPayTraceTransactionRecord();
			try
			{

			} catch(Exception ex)
			{

			}

			return record;
		}

		public PayTraceRedirectResponse Refund(IInvoice invoice, IPayment payment, decimal amount)
		{
			throw new NotImplementedException();
		}
		
		internal PayTraceRedirectTransactionRecord DoCheckoutPayment(IInvoice invoice, IPayment payment, string token, PayTraceRedirectTransactionRecord record)
		{			
			try
			{
				
			} catch(Exception ex)
			{
				
			}

			return record; 
		}

		/// <summary>
		/// Performs setup tasks for the checkout.
		/// </summary>
		/// <param name="invoice">The invoice.</param>
		/// <param name="payment">The payment.</param>
		/// <param name="returnUrl">The return URL.</param>
		/// <param name="cancelUrl">The cancel URL.</param>
		/// <returns></returns>
		public virtual PayTraceRedirectTransactionRecord SetCheckout(IInvoice invoice, IPayment payment, string returnUrl, string cancelUrl)
		{
			var record = new PayTraceRedirectTransactionRecord
			{
				Success = true,
				Data = { Authorized = false }			
				
			};
			try
			{
				record.SetCheckout = new PayTraceRedirectResponse();
				if (record.SetCheckout.Success())
				{
					//record.Data.Token = response.Token;
					//record.SetCheckout.RedirectUrl = GetRedirectUrl(response.Token);
				}
			}
			catch (Exception ex)
			{
				LogHelper.Error(typeof(PayTraceRedirectService), ex.Message, ex);
			}
			
			
			//var factory = new PayTracePaymentDetailsTypeFactory(new PayTraceFactorySettings { WebsiteUrl = _websiteUrl });
			//var paymentDetailsType = factory.Build(invoice, PaymentActionCodeType.ORDER);

			// The API requires this be in a list
			//var paymentDetailsList = new List<PaymentDetailsType>() { paymentDetailsType };

			// Checkout details
			//var ecDetails = new SetCheckoutRequestDetailsType()
			//{
			//	ReturnURL = returnUrl,
			//	CancelURL = cancelUrl,
			//	PaymentDetails = paymentDetailsList,
			//	AddressOverride = "1"
			//};

			// Trigger the event to allow for overriding ecDetails
			//var ecdOverride = new PayTraceRedirectCheckoutRequestDetailsOverride(invoice, payment, ecDetails);
			//SettingCheckoutRequestDetails.RaiseEvent(new ObjectEventArgs<PayTraceRedirectCheckoutRequestDetailsOverride>(ecdOverride), this);

			// The CheckoutRequest
			//var request = new SetCheckoutRequestType
			//{
			//	Version = Version,
			//	SetCheckoutRequestDetails = ecdOverride.CheckoutDetails
			//};

			// Crete the wrapper for  Checkout
			//var wrapper = new SetCheckoutReq
			//{
			//	SetCheckoutRequest = request
			//};

			//try
			//{
			//	var service = GetPayTraceService();
			//	var response = service.SetCheckout(wrapper);

			//record.SetCheckout = _responseFactory.Build(response, response.Token);
			//	if (record.SetCheckout.Success())
			//	{
			//		record.Data.Token = response.Token;
			//		record.SetCheckout.RedirectUrl = GetRedirectUrl(response.Token);
			//	}
			//	else
			//	{
			//		foreach (var et in record.SetCheckout.ErrorTypes)
			//		{
			//			var code = et.ErrorCode;
			//			var sm = et.ShortMessage;
			//			var lm = et.LongMessage;
			//			MultiLogHelper.Warn<PayTraceRedirectCheckoutService>(string.Format("{0} {1} {2}", code, lm, sm));
			//		}

			//		record.Success = false;
			//	}
			//}
			//catch (Exception ex)
			//{
			//	record.Success = false;
			//	record.SetCheckout = _responseFactory.Build(ex);
			//}

			return record;
		}
	}
}
