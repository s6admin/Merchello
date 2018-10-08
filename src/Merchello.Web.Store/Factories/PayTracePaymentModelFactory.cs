
namespace Merchello.Web.Store.Factories
{
	using Core.Gateways.Payment;

	using Merchello.Core;
	using Merchello.Core.Models;
	using Merchello.Providers;
	using Merchello.Providers.Models;
	using Merchello.Providers.Payment.Braintree.Provider;
	using Merchello.Providers.Payment.Braintree.Services;
	using Merchello.Providers.Payment.PayTrace.Provider;
	using Merchello.Web.Factories;
	using Merchello.Web.Store.Models;
	using Umbraco.Core.Logging;

	public class PayTracePaymentModelFactory<TPaymentModel> : CheckoutPaymentModelFactory<TPaymentModel>
		where TPaymentModel : PayTraceEncryptedPaymentModel, new ()
	{
		
		public PayTracePaymentModelFactory()
		{
			var provider = (PayTracePaymentGatewayProvider)MerchelloContext.Current.Gateways.Payment.GetProviderByKey(Merchello.Providers.Constants.PayTrace.GatewayProviderSettingsKey);
		}

		protected override TPaymentModel OnCreate(TPaymentModel model, ICustomerBase customer, IPaymentMethod paymentMethod)
		{

			SetBillingAddress(model, customer);				

			return base.OnCreate(model, customer, paymentMethod);
		}

		protected override TPaymentModel OnCreate(TPaymentModel model, ICustomerBase customer, IPaymentMethod paymentMethod, IPaymentResult attempt)
		{

			SetBillingAddress(model, customer);

			return base.OnCreate(model, customer, paymentMethod, attempt);
		}

		private void SetBillingAddress(TPaymentModel model, ICustomerBase customer)
		{
			IAddress ba = customer.ExtendedData.GetAddress(AddressType.Billing);

			if (ba == null)
			{
				LogHelper.Error(typeof(PayTracePaymentModelFactory<TPaymentModel>), "Could not retrieve Billing Address for PayTrace payment. ", new System.Exception("Could not retrieve Billing Address for customer " + customer.Key + " order number " + model.OrderNumber + " at PayTrace payment step. "));
			}
			else
			{
				model.BillingAddress = new Models.PayTraceBillingAddress();
				model.BillingAddress.City = ba.Locality;
				model.BillingAddress.Country = ba.CountryCode;
				model.BillingAddress.Name = ba.Name;
				model.BillingAddress.State = ba.Region;
				model.BillingAddress.StreetAddress = ba.Address1;
				model.BillingAddress.StreetAddress2 = ba.Address2;
				model.BillingAddress.Zip = ba.PostalCode;
			}
		}
	}
}
