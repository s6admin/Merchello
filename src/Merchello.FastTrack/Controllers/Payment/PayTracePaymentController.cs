
namespace Merchello.FastTrack.Controllers.Payment
{
	using System.Web.Mvc;
	using Core.Gateways;
	using Models.Payment;
	using Umbraco.Core;
	using Web.Controllers;
	using System;
	using Core.Gateways.Payment;
	using Providers.Payment.PurchaseOrder;
	using Umbraco.Web.Mvc;
	using Web.Models.Ui;
	using Providers;
	using Web.Models.Ui.Async;

	// S6 This is intended for use with the front-end PayTrace provider, not the redirect provider

	[PluginController("FastTrack")]
	[GatewayMethodUi("PayTrace.PurchaseOrder")]
	public class PayTracePaymentController : CheckoutPaymentControllerBase<PayTracePaymentModel>
	{
		/// <summary>
		/// Handles the redirection for the receipt.
		/// </summary>
		/// <param name="model">
		/// The <see cref="FastTrackPaymentModel"/>.
		/// </param>
		/// <returns>
		/// The <see cref="ActionResult"/>.
		/// </returns>
		protected override ActionResult HandlePaymentSuccess(PayTracePaymentModel model)
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
		public virtual ActionResult Process(PayTracePaymentModel model)
		{
			try
			{
				var paymentMethod = this.CheckoutManager.Payment.GetPaymentMethod();

				// Create the processor argument collection, where we'll pass in the purchase order
				var args = new ProcessorArgumentCollection
				{
					{Merchello.Providers.Constants.PurchaseOrder.PoStringKey, model.OrderNumber}
				};

				var attempt = this.CheckoutManager.Payment.AuthorizeCapturePayment(paymentMethod.Key, args);

				var resultModel = this.CheckoutPaymentModelFactory.Create(CurrentCustomer, paymentMethod, attempt);

				// merge the models so we can be assured that any hidden values are passed on
				model.ViewData = resultModel.ViewData;

				// Send the notification
				HandleNotificiation(model, attempt);

				return this.HandlePaymentSuccess(model);
			}
			catch (Exception ex)
			{
				return this.HandlePaymentException(model, ex);
			}
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

			return view.IsNullOrWhiteSpace() ? this.PartialView(model) : this.PartialView(view, model);
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
		protected virtual PaymentResultAsyncResponse GetAsyncResponse(PayTracePaymentModel model)
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

