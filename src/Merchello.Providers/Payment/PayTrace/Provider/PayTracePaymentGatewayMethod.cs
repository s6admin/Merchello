﻿using System;
using System.Linq;
using Merchello.Core;
using Merchello.Core.Gateways;
using Merchello.Core.Gateways.Payment;
using Merchello.Core.Models;
using Merchello.Core.Services;
using Umbraco.Core;
using Merchello.Providers.Payment.PurchaseOrder;
using Merchello.Providers;
using System.Diagnostics;
using Newtonsoft.Json;
using static Merchello.Providers.Constants.PayTrace;
using Merchello.Providers.Payment.PayTrace.Models;

namespace Merchello.Providers.Payment.PayTrace.Provider
{
	[GatewayMethodUi("PayTrace.PurchaseOrder")]
	[PaymentGatewayMethod("PayTrace Method Editors",
	   "",
	   "",
	   "~/App_Plugins/MerchelloProviders/views/dialogs/voidpayment.confirm.html",
	   "")]
	public class PayTracePaymentGatewayMethod : PaymentGatewayMethodBase, IPayTracePaymentGatewayMethod
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PayTracePaymentGatewayMethod"/> class.
		/// </summary>
		/// <param name="gatewayProviderService">
		/// The gateway provider service.
		/// </param>
		/// <param name="paymentMethod">
		/// The payment method.
		/// </param>
		public PayTracePaymentGatewayMethod(IGatewayProviderService gatewayProviderService, IPaymentMethod paymentMethod)
            : base(gatewayProviderService, paymentMethod)
        {
		}

		/// <summary>
		/// Does the actual work of creating and processing the payment
		/// </summary>
		/// <param name="invoice">The <see cref="IInvoice"/></param>
		/// <param name="args">Any arguments required to process the payment.</param>
		/// <returns>The <see cref="IPaymentResult"/></returns>
		protected override IPaymentResult PerformAuthorizePayment(IInvoice invoice, ProcessorArgumentCollection args)
		{
			throw new NotImplementedException();
			
			// S6 Demo PO code, not for production
			//var po = args.AsPurchaseOrderFormData();

			//var payment = GatewayProviderService.CreatePayment(PaymentMethodType.PurchaseOrder, invoice.Total, PaymentMethod.Key);
			//payment.CustomerKey = invoice.CustomerKey;
			//payment.PaymentMethodName = PaymentMethod.Name;
			//payment.ReferenceNumber = PaymentMethod.PaymentCode + "-" + invoice.PrefixedInvoiceNumber();
			//payment.Collected = false;
			//payment.Authorized = true;

			//if (string.IsNullOrEmpty(po.PurchaseOrderNumber))
			//{
			//	return new PaymentResult(Attempt<IPayment>.Fail(payment, new Exception("Error Purchase Order Number is empty")), invoice, false);
			//}

			//invoice.PoNumber = po.PurchaseOrderNumber;
			//MerchelloContext.Current.Services.InvoiceService.Save(invoice);

			//GatewayProviderService.Save(payment);

			//// In this case, we want to do our own Apply Payment operation as the amount has not been collected -
			//// so we create an applied payment with a 0 amount.  Once the payment has been "collected", another Applied Payment record will
			//// be created showing the full amount and the invoice status will be set to Paid.
			//GatewayProviderService.ApplyPaymentToInvoice(payment.Key, invoice.Key, AppliedPaymentType.Debit, string.Format("To show promise of a {0} payment", PaymentMethod.Name), 0);

			////// If this were using a service we might want to store some of the transaction data in the ExtendedData for record
			//////payment.ExtendData

			//return new PaymentResult(Attempt.Succeed(payment), invoice, false);
		}

		/// <summary>
		/// Does the actual work of authorizing and capturing a payment. This is used with the PayTrace encrypted JSON provider, not the redirect provider.
		/// </summary>
		/// <param name="invoice">The <see cref="IInvoice"/></param>
		/// <param name="amount">The amount to capture</param>
		/// <param name="args">Any arguments required to process the payment.</param>
		/// <returns>The <see cref="IPaymentResult"/></returns>
		protected override IPaymentResult PerformAuthorizeCapturePayment(IInvoice invoice, decimal amount, ProcessorArgumentCollection args)
		{
						
			var payment = GatewayProviderService.CreatePayment(PaymentMethodType.CreditCard, amount, PaymentMethod.Key);
			payment.CustomerKey = invoice.CustomerKey;
			payment.PaymentMethodName = PaymentMethod.Name;
			payment.ReferenceNumber = PaymentMethod.PaymentCode + "-" + invoice.PrefixedInvoiceNumber();			
			
			string token = args.ContainsKey("access_token") ? args.First(x => x.Key == "access_token").Value : string.Empty;
            string OAuth = String.Format("Bearer {0}", token); // paytraceTokenResult.AccessToken
						
			KeyedSaleRequest requestKeyedSale = new KeyedSaleRequest();
						
			KeyedSaleGenerator keyedSaleGenerator = new KeyedSaleGenerator();
						
			requestKeyedSale = PopulateRequestData(requestKeyedSale, args);
			
			var keyedSaleResult = keyedSaleGenerator.KeyedSaleTrans(OAuth, requestKeyedSale);

			if (keyedSaleResult != null && keyedSaleResult.HttpErrorMessage != null && keyedSaleResult.Success == false)
			{

				// Http Error(s) processing payment
				string errorMsg = "Http Error Code & Error : " + keyedSaleResult.HttpErrorMessage + "<br/>";
				errorMsg += "Success : " + keyedSaleResult.Success + "<br/>";
				errorMsg += "response_code : " + keyedSaleResult.ResponseCode + "<br/>";
                errorMsg += "status_message : " + keyedSaleResult.StatusMessage + "<br/>";
				errorMsg += "external_transaction_id : " + keyedSaleResult.ExternalTransactionId + "<br/>";
				errorMsg += "masked_card_number : " + keyedSaleResult.MaskedCardNumber + "<br/>";
				errorMsg += " API errors : " + "<br/>";
				
				// Check the actual API errors with appropriate code	
				if(keyedSaleResult.TransactionErrors != null && keyedSaleResult.TransactionErrors.Any())
				{		
					foreach (var item in keyedSaleResult.TransactionErrors)
					{
						// to read Error message with each error code in array.
						foreach (var errorMessage in (string[])item.Value)
						{
							errorMsg += item.Key + "=" + errorMessage + "<br/>";
						}
					}
				}
				return new PaymentResult(Attempt<IPayment>.Fail(payment, new Exception(errorMsg)), invoice, false);
			}
			else
			{
				// TODO Handle Other Response Codes: https://developers.paytrace.com/support/home#14000041297 Please refer to PayTrace-HTTP Status and Error Codes page for possible errors and Response Codes

				// For transaction successfully approved 
				if (keyedSaleResult.ResponseCode == 101 && keyedSaleResult.Success == true)
				{
					string successMsg = "Keyed sale: " + "Success!";

					payment.ExtendedData.SetValue(ExtendedDataKeys.PayTraceTransaction, "1"); // Simply used to help identify the type of data in the payment object in the front-end website 
					payment.ExtendedData.SetValue(ProcessorArgumentsKeys.InternalTokenKey, token);
					//payment.ExtendedData.SetValue("token_type", paytraceTokenResult.TokenType); // Probably not concerned with this value
					payment.ExtendedData.SetValue(ResponseKeys.ResponseCode, keyedSaleResult.ResponseCode.ToString());
					payment.ExtendedData.SetValue(ResponseKeys.TransactionId, keyedSaleResult.TransactionId.ToString());
					payment.ExtendedData.SetValue(ProcessorArgumentsKeys.BillingAddressName, requestKeyedSale.ObjBillingAddress.Name);
					payment.ExtendedData.SetValue(ResponseKeys.ApprovalCode, keyedSaleResult.ApprovalCode);
					payment.ExtendedData.SetValue(ResponseKeys.ApprovalMessage, keyedSaleResult.ApprovalMessage.Replace("  ", " ")); // Some messages contain multiple blank spaces so collapse white-space to single before saving
					payment.ExtendedData.SetValue(ResponseKeys.AvsResponse, keyedSaleResult.AvsResponse);
					payment.ExtendedData.SetValue(ResponseKeys.CscResponse, keyedSaleResult.CscResponse);
					payment.ExtendedData.SetValue(ResponseKeys.ExternalTransactionid, keyedSaleResult.ExternalTransactionId);
					payment.ExtendedData.SetValue(ResponseKeys.MaskedCardNumber, keyedSaleResult.MaskedCardNumber);

					payment.Collected = true;
					payment.Authorized = true;

					MerchelloContext.Current.Services.InvoiceService.Save(invoice);

					this.GatewayProviderService.Save(payment);
										
					this.GatewayProviderService.ApplyPaymentToInvoice(payment.Key, invoice.Key, AppliedPaymentType.Debit, successMsg, amount);

					return new PaymentResult(Attempt<IPayment>.Succeed(payment), invoice, true);
				}
				else
				{
					// Do your code here based on the response_code - use the PayTrace http status and error page for reference
					// Do your code for any additional verification - avs_response and csc_response
										

					string errorMsg = "Error : " + keyedSaleResult.HttpErrorMessage + "<br/>";
					return new PaymentResult(Attempt<IPayment>.Fail(payment, new Exception(errorMsg)), invoice, false);
				}

				// Do your code for any additional task(s)
			}
		}
		
		protected KeyedSaleRequest PopulateRequestData(KeyedSaleRequest requestKeyedSale, ProcessorArgumentCollection args)
		{
			requestKeyedSale.Amount = double.Parse(ArgValue(Constants.PayTrace.ProcessorArgumentsKeys.Amount, args));			
			requestKeyedSale.ObjCreditCard = JsonConvert.DeserializeObject<PayTraceCreditCard>(ArgValue(Constants.PayTrace.ProcessorArgumentsKeys.PayTraceCreditCard, args));
			requestKeyedSale.ObjBillingAddress = JsonConvert.DeserializeObject<PayTraceBillingAddress>(ArgValue(Constants.PayTrace.ProcessorArgumentsKeys.PayTraceBillingAddress, args));
			requestKeyedSale.Csc = ArgValue(Constants.PayTrace.ProcessorArgumentsKeys.EncryptedCreditCardCode, args);
			
			return requestKeyedSale;

		}
			
		/// <summary>
		/// Does the actual work capturing a payment
		/// </summary>
		/// <param name="invoice">The <see cref="IInvoice"/></param>
		/// <param name="payment">The previously Authorize payment to be captured</param>
		/// <param name="amount">The amount to capture</param>
		/// <param name="args">Any arguments required to process the payment.</param>
		/// <returns>The <see cref="IPaymentResult"/></returns>
		protected override IPaymentResult PerformCapturePayment(IInvoice invoice, IPayment payment, decimal amount, ProcessorArgumentCollection args)
		{
			throw new NotImplementedException();

			// S6 Demo PO code, not for production
			//// We need to determine if the entire amount authorized has been collected before marking
			//// the payment collected.
			//var appliedPayments = GatewayProviderService.GetAppliedPaymentsByPaymentKey(payment.Key);
			//var applied = appliedPayments.Sum(x => x.Amount);

			//payment.Collected = (amount + applied) == payment.Amount;
			//payment.Authorized = true;

			//GatewayProviderService.Save(payment);

			//GatewayProviderService.ApplyPaymentToInvoice(payment.Key, invoice.Key, AppliedPaymentType.Debit, "Purchase Order Payment", amount);

			//return new PaymentResult(Attempt<IPayment>.Succeed(payment), invoice, CalculateTotalOwed(invoice).CompareTo(amount) <= 0);
		}

		/// <summary>
		/// Does the actual work of refunding a payment
		/// </summary>
		/// <param name="invoice">The <see cref="IInvoice"/></param>
		/// <param name="payment">The previously Authorize payment to be captured</param>
		/// <param name="amount">The amount to be refunded</param>
		/// <param name="args">Any arguments required to process the payment.</param>
		/// <returns>The <see cref="IPaymentResult"/></returns>
		protected override IPaymentResult PerformRefundPayment(IInvoice invoice, IPayment payment, decimal amount, ProcessorArgumentCollection args)
		{

			throw new NotImplementedException();

			// S6 Demo PO code, not for production
			//foreach (var applied in payment.AppliedPayments())
			//{
			//	applied.TransactionType = AppliedPaymentType.Refund;
			//	applied.Amount = 0;
			//	applied.Description += " - Refunded";
			//	GatewayProviderService.Save(applied);
			//}

			//payment.Amount = payment.Amount - amount;

			//if (payment.Amount != 0)
			//{
			//	GatewayProviderService.ApplyPaymentToInvoice(payment.Key, invoice.Key, AppliedPaymentType.Debit, "To show partial payment remaining after refund", payment.Amount);
			//}

			//GatewayProviderService.Save(payment);

			//return new PaymentResult(Attempt<IPayment>.Succeed(payment), invoice, false);
		}

		private string ArgValue(string propertyName, ProcessorArgumentCollection args)
		{
			return args.ContainsKey(propertyName) ? args[propertyName] : string.Empty;
		}
	}
}

