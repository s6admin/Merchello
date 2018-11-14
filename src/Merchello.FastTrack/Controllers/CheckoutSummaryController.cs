namespace Merchello.FastTrack.Controllers
{
	using System.Web.Mvc;
	using Merchello.FastTrack.Factories;
	using Merchello.FastTrack.Models;
	using MC = Merchello.Providers.Constants;
	using Merchello.Web.Controllers;
	using Merchello.Web.Factories;
	using Merchello.Web.Models.Ui;
	using Merchello.Web.Store.Models;

	using Umbraco.Core;
	using Umbraco.Web.Mvc;
	using System;

	/// <summary>
	/// The default checkout summary controller.
	/// </summary>
	[PluginController("FastTrack")]
    public class CheckoutSummaryController : CheckoutSummaryControllerBase<FastTrackCheckoutSummaryModel, FastTrackBillingAddressModel, StoreAddressModel, StoreLineItemModel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CheckoutSummaryController"/> class.
        /// </summary>
        public CheckoutSummaryController()
            : base(
                  new FastTrackCheckoutSummaryModelFactory(),
                  new CheckoutContextSettingsFactory())
        {
        }

        /// <summary>
        /// Renders the Basket Summary.
        /// </summary>
        /// <param name="view">
        /// The optional view.
        /// </param>
        /// <returns>
        /// The <see cref="ActionResult"/>.
        /// </returns>
        [ChildActionOnly]
        public override ActionResult BasketSummary(string view = "")
        {
            var model = CheckoutSummaryFactory.Create(Basket, CheckoutManager);

            // EDIT ADDRESS BUTTON VISIBILITY
            // FastTrack implementation uses the notion of checkout stages in the UI
            // to determine what to display and the order in which to display them.  We can 
            // determine the stage by validating models at various points
            if (ValidateModel(model.ShippingAddress))
            {
                model.CheckoutStage = CheckoutStage.ShipRateQuote;
            }
            else if (ValidateModel(model.BillingAddress))
            {
                model.CheckoutStage = CheckoutStage.ShippingAddress;
            }
            
            return view.IsNullOrWhiteSpace() ? this.PartialView(model) : this.PartialView(view, model);
        }

		// S6
		public override ActionResult SalesReceipt(string view = "")
		{

			ActionResult ar = base.SalesReceipt(view);
			PartialViewResult vr = null;
			
			try {

				vr = ar as PartialViewResult;
				// https://our.umbraco.com/packages/collaboration/merchello/merchello/77524-reinstate-basket-after-failed-payment
				// If there are failed payment attempts for a PayTrace payment gateway, this provides an opportunity to display a customer message on the receipt page before resetting the failed attempts flag
				if (CheckoutManager.Customer.Context.Customer.ExtendedData.ContainsKey(MC.PayTrace.ExtendedDataKeys.FailedAttempts))
				{
					CheckoutManager.Customer.Context.Customer.ExtendedData.RemoveValue(MC.PayTrace.ExtendedDataKeys.FailedAttempts);					
					if(vr.TempData != null)
					{
						vr.TempData.Add(MC.PayTrace.ExtendedDataKeys.FailedAttempts, true); // Include a temporary key so inherited front-end controller(s) can respond if desired
					}

					/* 
						Force Basket to empty because the default Merchello behavior is to keep content on payment attempt failure regardless of config settings
						If an order has been accepted by the office even after payment failure, the customer should not see those items in their active cart anymore
					*/
					Basket.Empty();
				}

				return vr;

			} catch(Exception ex)
			{
				// TODO Log if desired
			}

			return ar;
		}
	}
}