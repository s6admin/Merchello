
// S6 Payment Providers are primarily responsible for wiring a payment portal into the Merchello Gateway dashboard UI.
namespace Merchello.Providers.Payment.PayTrace.Provider
{

	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Merchello.Core.Gateways;
	using Merchello.Core.Gateways.Payment;
	using Merchello.Core.Models;
	using Merchello.Core.Services;
	using Umbraco.Core.Cache;
	using Umbraco.Core.Logging;
	using Merchello.Providers.Payment.PayTrace.Services;
	using Merchello.Core.Logging;
	using Merchello.Providers.Payment.PayTrace.Models;
	using Newtonsoft.Json;
	
	using Merchello.Providers.Models;
	using Merchello.Providers.Payment.Models;
	using Merchello.Providers.Payment.PayPal.Models;
	using Merchello.Providers.Payment.PayPal.Services;

	using Constants = Merchello.Providers.Constants;
	
	//[GatewayProviderEditor(...)]
	[GatewayProviderActivation("TODO-KEY", "PayTrace Payment Provider", "PayTrace Payment Provider")]
	[ProviderSettingsMapper(Constants.PayTrace.ExtendedDataKeys.ProviderSettings, typeof(PayTraceProviderSettings))]
	public class PayTracePaymentGatewayProvider : PaymentGatewayProviderBase, IPayTracePaymentGatewayProvider
	{
		#region AvailableResources

		/// <summary>
		/// The available resources.
		/// </summary>
		internal static readonly IEnumerable<IGatewayResource> AvailableResources = new List<IGatewayResource>
		{			
			new GatewayResource("PayTraceOrder", "PayTrace Order")
		};

		#endregion

		/// <summary>
		/// Initializes a new instance of the <see cref="PayTracePaymentGatewayProvider"/> class.
		/// </summary>
		/// <param name="gatewayProviderService">
		/// The gateway provider service.
		/// </param>
		/// <param name="gatewayProviderSettings">
		/// The gateway provider settings.
		/// </param>
		/// <param name="runtimeCacheProvider">
		/// The runtime cache provider.
		/// </param>
		public PayTracePaymentGatewayProvider(
			IGatewayProviderService gatewayProviderService,
			IGatewayProviderSettings gatewayProviderSettings,
			IRuntimeCacheProvider runtimeCacheProvider) 
            : base(gatewayProviderService, gatewayProviderSettings, runtimeCacheProvider)
        {
		}

		/// <summary>
		/// Creates a <see cref="IPaymentGatewayMethod"/>
		/// </summary>
		/// <param name="name">The name of the payment method</param>
		/// <param name="description">The description of the payment method</param>
		/// <returns>A <see cref="IPaymentGatewayMethod"/></returns>
		public IPaymentGatewayMethod CreatePaymentMethod(string name, string description)
		{
			return CreatePaymentMethod(AvailableResources.First(), name, description);
		}

		/// <summary>
		/// Creates a <see cref="IPaymentGatewayMethod"/>
		/// </summary>
		/// <param name="gatewayResource">
		/// The gateway Resource.
		/// </param>
		/// <param name="name">
		/// The name of the payment method
		/// </param>
		/// <param name="description">
		/// The description of the payment method
		/// </param>
		/// <returns>
		/// A <see cref="IPaymentGatewayMethod"/>
		/// </returns>
		public override IPaymentGatewayMethod CreatePaymentMethod(IGatewayResource gatewayResource, string name, string description)
		{
			// assert gateway resource is still available
			var available = this.ListResourcesOffered()
				.FirstOrDefault(x => x.ServiceCode == gatewayResource.ServiceCode);
			if (available == null) throw new InvalidOperationException("GatewayResource has already been assigned");

			var attempt = this.GatewayProviderService.CreatePaymentMethodWithKey(
				this.GatewayProviderSettings.Key,
				name,
				description,
				available.ServiceCode);


			if (attempt.Success)
			{
				this.PaymentMethods = null;

				return GetPaymentGatewayMethodByPaymentCode(available.ServiceCode);
			}

			LogHelper.Error<PayTracePaymentGatewayProvider>(
				string.Format(
					"Failed to create a payment method name: {0}, description {1}, paymentCode {2}",
					name,
					description,
					available.ServiceCode),
				attempt.Exception);

			throw attempt.Exception;
		}

		/// <summary>
		/// Gets a <see cref="IPaymentGatewayMethod"/> by it's unique 'key'
		/// </summary>
		/// <param name="paymentMethodKey">The key of the <see cref="IPaymentMethod"/></param>
		/// <returns>A <see cref="IPaymentGatewayMethod"/></returns>
		public override IPaymentGatewayMethod GetPaymentGatewayMethodByKey(Guid paymentMethodKey)
		{
			var paymentMethod = this.PaymentMethods.FirstOrDefault(x => x.Key == paymentMethodKey);

			if (paymentMethod == null) throw new NullReferenceException("PaymentMethod not found");

			return GetPaymentGatewayMethodByPaymentCode(paymentMethod.PaymentCode);
		}

		/// <summary>
		/// Gets a <see cref="IPaymentGatewayMethod"/> by it's payment code
		/// </summary>
		/// <param name="paymentCode">The payment code of the <see cref="IPaymentGatewayMethod"/></param>
		/// <returns>A <see cref="IPaymentGatewayMethod"/></returns>
		public override IPaymentGatewayMethod GetPaymentGatewayMethodByPaymentCode(string paymentCode)
		{
			var paymentMethod = this.PaymentMethods.FirstOrDefault(x => x.PaymentCode == paymentCode);

			if (paymentMethod != null)
			{
				switch (paymentCode)
				{
					case Constants.PayTrace.PaymentCodes.Checkout:
						return new PayTracePaymentGatewayMethod(
							this.GatewayProviderService,
							paymentMethod,
							GetPayTraceApiService());
						//// TODO add additional payment methods here 
				}
			}

			var logData = MultiLogger.GetBaseLoggingData();
			logData.AddCategory("GatewayProviders");
			logData.AddCategory("PayTrace");

			var nullRef =
				new NullReferenceException(string.Format("PaymentMethod not found for payment code: {0}", paymentCode));
			MultiLogHelper.Error<PayTracePaymentGatewayProvider>(
				"Failed to find payment method for payment code",
				nullRef,
				logData);

			throw nullRef;
		}

		/// <summary>
		/// Gets the <see cref="IPayTraceApiService"/>.
		/// </summary>
		/// <returns>
		/// The <see cref="IPayTraceApiService"/>.
		/// </returns>
		private IPayTraceApiService GetPayTraceApiService()
		{
			// S6 Extracted so we don't need to modify the shared Merchello.Providers.ProviderSettingsExtensions.cs
			PayTraceProviderSettings settings;
			if (this.ExtendedData.ContainsKey(Constants.PayTrace.ExtendedDataKeys.ProviderSettings))
			{
				var json = this.ExtendedData.GetValue(Constants.PayTrace.ExtendedDataKeys.ProviderSettings);
				settings = JsonConvert.DeserializeObject<PayTraceProviderSettings>(json);
			}
			else
			{
				settings = new PayTraceProviderSettings();
			}

			return new PayTraceApiService(settings);
			
			//return new PayTraceApiService(this.ExtendedData.GetPayTraceProviderSettings());
		}

		/// <summary>
		/// Returns a list of remaining available resources
		/// </summary>
		/// <returns>
		/// The collection of <see cref="IGatewayResource"/>.
		/// </returns>
		public override IEnumerable<IGatewayResource> ListResourcesOffered()
		{
			return AvailableResources;
		}
	}
}
