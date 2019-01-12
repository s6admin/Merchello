
namespace Merchello.FastTrack.Controllers.Payment
{
	using System.Web.Mvc;
	using Core.Gateways;
	using Models.Payment;
	using Umbraco.Core;
	using Web.Controllers;
	using System;
	using Newtonsoft.Json;
	using Core.Gateways.Payment;
	using MC = Merchello.Providers.Constants;
	using Umbraco.Web.Mvc;
	using Web.Models.Ui;
	using Providers;
	using Web.Models.Ui.Async;
	using Web.Store.Models;
	using Core.Models;
	using Web.Store.Factories;
	using Web.Factories;
	using Umbraco.Core.Logging;
	using Core.Logging;
	using Providers.Payment.PayTrace;

	// S6 This is for the front-end encryption PayTrace provider, not the redirect provider

	[PluginController("FastTrack")]
	[GatewayMethodUi("PayTrace.PurchaseOrder")]
	public class PayTracePaymentController : CheckoutPaymentControllerBase<PayTraceEncryptedPaymentModel>		
	{

		public PayTracePaymentController()
			: base(new PayTracePaymentModelFactory<PayTraceEncryptedPaymentModel>())
		{
		}

		/// <summary>
		/// Renders the Purchase Order payment form.
		/// </summary>
		/// <param name="view">
		/// The optional view.
		/// </param>
		/// <returns>
		/// The <see cref="ActionResult"/>.
		/// </returns>
		[ChildActionOnly]
		[GatewayMethodUi("PayTrace.PurchaseOrder")]
		public override ActionResult PaymentForm(string view = "")
		{
			var paymentMethod = this.CheckoutManager.Payment.GetPaymentMethod();
			if (paymentMethod == null) return this.InvalidCheckoutStagePartial();

			var model = this.CheckoutPaymentModelFactory.Create(CurrentCustomer, paymentMethod);

			// Set any eComm or CMS properties we don't want to scope into the PayTrace Factory

			// Preparing the invoice saves and generates a key. This should only be done once per checkout unless the basket is invalidated
			IInvoice invoice = PrepareInvoiceOnce();
			if (invoice != null)
			{
				model.Amount = decimal.ToDouble(invoice.Total);
				model.TaxAmount = invoice.TotalTax();
				//model.OrderNumber = invoice.PoNumber;
			}

			return view.IsNullOrWhiteSpace() ? this.PartialView(model) : this.PartialView(view, model);
		}

		/// <summary>
		/// Processes the PayTrace payment.
		/// </summary>
		/// <param name="model">
		/// The <see cref="ICheckoutPaymentModel"/>.
		/// </param>
		/// <returns>
		/// The <see cref="ActionResult"/>.
		/// </returns>
		[HttpPost]
		public virtual ActionResult Process(PayTraceEncryptedPaymentModel model)
		{
			try
			{
				
				#region PayTrace API

				PayTraceOAuthTokenGenerator tokenGenerator = new PayTraceOAuthTokenGenerator();
				var paytraceTokenResult = tokenGenerator.GetToken();

				// Determine whether OAuthToken is successful and make a request
				if (paytraceTokenResult.ErrorFlag != false)
				{
					string errorMsg = "Could not authenticate token with PayTrace for Invoice " + model.OrderNumber;
					if (paytraceTokenResult.ObjError != null)
					{
						errorMsg += paytraceTokenResult.ObjError.Error ?? string.Empty;
						errorMsg += paytraceTokenResult.ObjError.ErrorDescription ?? string.Empty;
						errorMsg += paytraceTokenResult.ObjError.HttpTokenError ?? string.Empty;
					}

					Exception ex = new Exception(errorMsg);
					LogHelper.Error(typeof(PayTracePaymentController), errorMsg, ex);

					return this.HandlePaymentException(model, ex);
				}
				else
				{
					model.PayTraceToken = paytraceTokenResult.AccessToken;
				}

				#endregion
				
				// Re-get any needed model values that aren't persisted in the paytrace paymentform
				var paymentMethod = this.CheckoutManager.Payment.GetPaymentMethod();

				IInvoice invoice = PrepareInvoiceOnce();
				
				if (invoice != null)
				{
					model.Amount = decimal.ToDouble(invoice.Total);
					model.TaxAmount = invoice.TotalTax();
				}

				var getModelForBilling = this.CheckoutPaymentModelFactory.Create(CurrentCustomer, paymentMethod);
				if(getModelForBilling != null)
				{
					model.BillingAddress = getModelForBilling.BillingAddress;
				}				
				
				var attempt = ProcessPayment(model);
								
				var resultModel = this.CheckoutPaymentModelFactory.Create(CurrentCustomer, paymentMethod, attempt); // This Create adds extra details to the View based on the success/failure of the payment attempt

				// merge the models so we can be assured that any hidden values are passed on
				model.ViewData = resultModel.ViewData;

				// Set the invoice key in the customer context (cookie) S6 Moved up from Success payment otherwise failed payment attempts will create multiple invoices
				if (model.ViewData != null && !model.ViewData.InvoiceKey.Equals(Guid.Empty))
				{
					CustomerContext.SetValue("invoiceKey", model.ViewData.InvoiceKey.ToString()); // Appears as "merchCustCtxinvoiceKey"
				}

				if (attempt.Payment.Success)
				{
					
					// S6 Removed. TODO This fails because "view" is not set...causes customer-facing InvalidCheckoutStage error
					//HandleNotificiation(model, attempt);

					return this.HandlePaymentSuccess(model);
				} else
				{

					// S6 Removed. TODO This fails because "view" is not set...causes customer-facing InvalidCheckoutStage error
					//HandleNotificiation(model, attempt);

					return this.HandlePaymentException(model, attempt.Payment.Exception);
				}
							
				
			}
			catch (Exception ex)
			{
				return this.HandlePaymentException(model, ex);
			}
		}

		// Special method for ensuring an invoice is only prepared once during the PaymentForm and/or Process checkout since we allow multiple payment attempts in case of gateway failure(s)
		private IInvoice PrepareInvoiceOnce()
		{
			IInvoice invoice = null;
            Guid invoiceKey = Guid.Empty;

			// Check for an invoice key in customer context so duplicate invoices aren't created (ie.) during payment failures
			try
			{
				invoiceKey = new Guid(CustomerContext.GetValue("invoiceKey"));
			}
			catch (Exception ex)
			{

			}
			if (invoiceKey.Equals(Guid.Empty))
			{
				// Prepare invoice for initial payment attempt
				invoice = this.CheckoutManager.Payment.PrepareInvoice();
			}
			else
			{
				/* 
					An invoice key is already present in customer context which means a previous payment attempt was made and failed
					In this case we want to retrieve the existing invoice, not create duplicates for each payment attempt					
				*/
				invoice = CheckoutManager.Context.Services.InvoiceService.GetByKey(invoiceKey);
			}

			return invoice;
		}

		protected virtual IPaymentResult ProcessPayment(PayTraceEncryptedPaymentModel model, IInvoice invoice = null)
		{
			
			var paymentMethod = CheckoutManager.Payment.GetPaymentMethod();
			
			// Create the processor argument collection, where we'll pass in the purchase order
			var args = new ProcessorArgumentCollection();			
			args.Add("access_token", model.PayTraceToken);
			args.Add(MC.PayTrace.ProcessorArgumentsKeys.PayTraceCreditCard, JsonConvert.SerializeObject(model.CreditCard));
			args.Add(MC.PayTrace.ProcessorArgumentsKeys.PayTraceBillingAddress, JsonConvert.SerializeObject(model.BillingAddress));
			args.Add(MC.PayTrace.ProcessorArgumentsKeys.Amount, model.Amount.ToString()); // Amount includes FINAL cost including tax. Tax amount (below) is only the tax line item amount
			args.Add(MC.PayTrace.ProcessorArgumentsKeys.TaxAmount, model.TaxAmount.ToString());
			args.Add(MC.PayTrace.ProcessorArgumentsKeys.EncryptedCreditCardCode, model.CcCscEncrypted);
			//args.Add(MC.PayTrace.ProcessorArgumentsKeys.EncryptedCreditCardNumber, model.CreditCard.CcNumberEncrypted);
			//args.Add(MC.PayTrace.ProcessorArgumentsKeys.City)

			return invoice == null
					   ? CheckoutManager.Payment.AuthorizeCapturePayment(paymentMethod.Key, args)
					   : invoice.AuthorizeCapturePayment(paymentMethod.Key, args);
		}

		/// <summary>
		/// Handles the redirection for the receipt.
		/// </summary>
		/// <param name="model">
		/// The <see cref="FastTrackPaymentModel"/>.
		/// </param>
		/// <returns>
		/// The <see cref="ActionResult"/>.
		/// </returns>
		protected override ActionResult HandlePaymentSuccess(PayTraceEncryptedPaymentModel model)
		{
			
			// Set the invoice key in the customer context (cookie)
			if (model.ViewData != null && model.ViewData.Success)
			{
				CustomerContext.SetValue("invoiceKey", model.ViewData.InvoiceKey.ToString());
			}

			if (Request.IsAjaxRequest())
			{
				var json = Json(GetAsyncResponse(model));

				return json;
			}

			// Remove any previously-saved failed payment attempts if one actually does succeed						
			if (CheckoutManager.Customer.Context.Customer.ExtendedData.ContainsKey(MC.PayTrace.ExtendedDataKeys.FailedAttempts))
			{
				CheckoutManager.Customer.Context.Customer.ExtendedData.RemoveValue(MC.PayTrace.ExtendedDataKeys.FailedAttempts);
			}

			return model.ViewData.Success && !model.SuccessRedirectUrl.IsNullOrWhiteSpace() ?
				Redirect(model.SuccessRedirectUrl) :
				base.HandlePaymentSuccess(model);
		}


		protected override ActionResult HandlePaymentException(PayTraceEncryptedPaymentModel model, Exception ex)
		{
			
			// Reset any credit card values so they are not pre-populated into the form if an error occurs (customers will need to re-enter their details for security purposes)			
			ModelState.Remove("CreditCard.ExpireMonth");
			ModelState.Remove("CreditCard.ExpireYear");
			ModelState.Remove("CreditCard.CcNumberEncrypted");
			model.CreditCard = new Merchello.Web.Store.Models.PayTraceCreditCard();
						
			ModelState.Remove("CcCscEncrypted");
			model.CcCscEncrypted = string.Empty;

			// Keep track of the failed attempts in the Customer data so the next checkout step is aware of the payment failure(s)
			int attempts = 1;
			ExtendedDataCollection ed = CheckoutManager.Customer.Context.Customer.ExtendedData;
						
			// Retrieve previous saved value if it exists
			if (ed.ContainsKey(MC.PayTrace.ExtendedDataKeys.FailedAttempts))
			{
				int.TryParse(ed.GetValue(MC.PayTrace.ExtendedDataKeys.FailedAttempts), out attempts); // Retrieve previous value
				attempts = attempts + 1; // Increment previous value				
			}

			ed.SetValue(MC.PayTrace.ExtendedDataKeys.FailedAttempts, attempts.ToString());
			ViewData[MC.PayTrace.ExtendedDataKeys.FailedAttempts] = attempts;
						
			LogHelper.Error(typeof(PayTracePaymentController), "Failed payment operation. Number of Attempts: " + attempts, ex);
			
			if (attempts > 2)
			{
				// After a certain number of failed attempts, submit order but do not record payment. notify customer their card has failed or been declined and that further action will be required to finalize their purchase
				CustomerContext.SetValue("invoiceKey", model.ViewData.InvoiceKey.ToString());

				// Keep FailedAttempts in CheckoutManager until receipt page so a proper message can be displayed to the user
				// Remove failed attempts data so it doesn't affect future orders
				//if (CheckoutManager.Customer.Context.Customer.ExtendedData.ContainsKey(MC.PayTrace.ExtendedDataKeys.FailedAttempts))
				//{
				//	CheckoutManager.Customer.Context.Customer.ExtendedData.RemoveValue(MC.PayTrace.ExtendedDataKeys.FailedAttempts);
				//}

				return !model.SuccessRedirectUrl.IsNullOrWhiteSpace() ? 
					Redirect(model.SuccessRedirectUrl) : 
					base.HandlePaymentSuccess(model);
            } else
			{
				// Return to payment form and allow customer to resubmit their payment details
				return CurrentUmbracoPage();								
			}						
			
			// base.handlepaymentexception logs error and throws hard exception which isn't desired in this case. allow customer order to be sent but notify them payment has been declined
			//return base.HandlePaymentException(model, ex);
		}
				
		/// <summary>
		/// Gets the <see cref="PaymentResultAsyncResponse"/> for the model.
		/// </summary>
		/// <param name="model">
		/// The <see cref="BraintreePaymentModel"/>.
		/// </param>
		/// <returns>
		/// The <see cref="PaymentResultAsyncResponse"/>.
		/// </returns>
		protected virtual PaymentResultAsyncResponse GetAsyncResponse(PayTraceEncryptedPaymentModel model)
		{
			var resp = new PaymentResultAsyncResponse
			{
				Success = model.ViewData.Success,
				InvoiceKey = model.ViewData.InvoiceKey,
				PaymentKey = model.ViewData.PaymentKey,
				ItemCount = GetBasketItemCountForDisplay(),
				PaymentMethodName = model.PaymentMethodName
			};

			foreach (var msg in model.ViewData.Messages) resp.Messages.Add(msg);

			return resp;
		}

		/// <summary>
		/// Gets the total basket count.
		/// </summary>
		/// <returns>
		/// The <see cref="int"/>.
		/// </returns>
		/// <remarks>
		/// This is generally used in navigations and labels.  Some implementations show the total number of line items while
		/// others show the total number of items (total sum of product quantities - default).
		/// 
		/// Method is used in Async responses to allow for easier HTML label updates 
		/// </remarks>
		protected virtual int GetBasketItemCountForDisplay()
		{
			return this.Basket.TotalQuantityCount;
		}
	}
}

