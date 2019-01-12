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
	using Merchello.Core.Gateways;
	using Merchello.Web.Controllers;
	using Merchello.Web.Store.Models;
	using Merchello.Web.Store.Factories;
	using Merchello.Core.Models;

	// S6 This is used for the PayTrace Redirect payment methods, not the Client-side Encryption JSON payment methods
	[PluginController("FastTrack")]
	[GatewayMethodUi("PayTrace.RedirectCheckout")]
	public class PayTraceRedirectPaymentController : CheckoutPaymentControllerBase<PayTraceRedirectPaymentModel>
	{

		public PayTraceRedirectPaymentController()
			: base(new PayTraceRedirectPaymentModelFactory<PayTraceRedirectPaymentModel>())
		{ }

		/// <summary>
		/// Responsible for rendering the payment form. There are no customer-submitted values in the Redirect form, it is just a notification that they will be taken off-site to complete their transaction after clicking the button
		/// </summary>
		/// <param name="view">The optional view.</param>
		/// <returns>
		/// The <see cref="ActionResult" />.
		/// </returns>
		[ChildActionOnly]
		[GatewayMethodUi("PayTrace.RedirectCheckout")]
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
				//model.OrderNumber = invoice.PoNumber;
			}

			return view.IsNullOrWhiteSpace() ? this.PartialView(model) : this.PartialView(view, model);
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

	}
}
