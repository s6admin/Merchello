
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

			IInvoice invoice = this.CheckoutManager.Payment.PrepareInvoice();

			if(invoice != null)
			{
				model.Amount = decimal.ToDouble(invoice.Total); 
			}
						
			//model.OrderNumber = invoice.PoNumber; // TODO Do we have an Invoice Order Number set by this time in checkout? It is optional for PayTrace so we shouldn't force creating one here if eComm naturally establishes one after the payment result has been received

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

				IInvoice invoice = this.CheckoutManager.Payment.PrepareInvoice();
				if (invoice != null)
				{
					model.Amount = decimal.ToDouble(invoice.Total);
				}

				var getModelForBilling = this.CheckoutPaymentModelFactory.Create(CurrentCustomer, paymentMethod);
				if(getModelForBilling != null)
				{
					model.BillingAddress = getModelForBilling.BillingAddress;
				}				

				var attempt = ProcessPayment(model);
								
				var resultModel = this.CheckoutPaymentModelFactory.Create(CurrentCustomer, paymentMethod, attempt); // This Create adds extra details to the View based on the success/failure of the payment attempt

				// merge the models so we can be assured that any hidden values are passed on
				//model.ViewData = resultModel.ViewData;

				// S6 Removed. TODO This fails because "view" is not set...causes customer-facing InvalidCheckoutStage error
				//HandleNotificiation(model, attempt);
				
				return this.HandlePaymentSuccess(model);
			}
			catch (Exception ex)
			{
				return this.HandlePaymentException(model, ex);
			}
		}

		protected virtual IPaymentResult ProcessPayment(PayTraceEncryptedPaymentModel model, IInvoice invoice = null)
		{
			
			var paymentMethod = CheckoutManager.Payment.GetPaymentMethod();

			// Create the processor argument collection, where we'll pass in the purchase order
			var args = new ProcessorArgumentCollection();			
			args.Add("access_token", model.PayTraceToken);
			args.Add(MC.PayTrace.ProcessorArgumentsKeys.PayTraceCreditCard, JsonConvert.SerializeObject(model.CreditCard));
			args.Add(MC.PayTrace.ProcessorArgumentsKeys.PayTraceBillingAddress, JsonConvert.SerializeObject(model.BillingAddress));
			args.Add(MC.PayTrace.ProcessorArgumentsKeys.Amount, model.Amount.ToString());
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
			if (model.ViewData.Success)
			{
				CustomerContext.SetValue("invoiceKey", model.ViewData.InvoiceKey.ToString());
			}

			if (Request.IsAjaxRequest())
			{
				var json = Json(GetAsyncResponse(model));

				return json;
			}

			return base.HandlePaymentSuccess(model);

			//return model.ViewData.Success && !model.SuccessRedirectUrl.IsNullOrWhiteSpace() ?
			//	Redirect(model.SuccessRedirectUrl) :
			//	base.HandlePaymentSuccess(model);
		}


		protected override ActionResult HandlePaymentException(PayTraceEncryptedPaymentModel model, Exception ex)
		{

			// TODO Any custom steps required to handle PayTrace integration and/or Customer experience

			// Reset any credit card values so they are not pre-populated into the form if an error occurs
			model.CreditCard = new Merchello.Web.Store.Models.PayTraceCreditCard();

			return base.HandlePaymentException(model, ex);
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

