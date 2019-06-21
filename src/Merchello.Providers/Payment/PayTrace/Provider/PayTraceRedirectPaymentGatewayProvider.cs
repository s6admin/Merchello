using System;
using System.Collections.Generic;
using System.Linq;

using Merchello.Core.Gateways;
using Merchello.Core.Gateways.Payment;
using Merchello.Core.Logging;
using Merchello.Core.Models;
using Merchello.Core.Services;
using Merchello.Providers.Models;
using Merchello.Providers.Payment.Models;
//using Merchello.Providers.Payment.PayTrace.Models;
//using Merchello.Providers.Payment.PayTrace.Services;

using Umbraco.Core.Cache;
using Umbraco.Core.Logging;

using Constants = Merchello.Providers.Constants;
using Merchello.Providers.Payment.PayTrace.Services;
using Merchello.Providers.Payment.PayTrace.Models;
using Newtonsoft.Json;

namespace Merchello.Providers.Payment.PayTrace.Provider
{
	/// <summary>
	/// Represents a PayTraceRedirectPaymentGatewayProvider
	/// </summary>
	//[ProviderSettingsMapper(Constants.PayTraceRedirect.ExtendedDataKeys.ProviderSettings, typeof(PayTraceProviderSettings))]
	//[GatewayProviderEditor("PayTrace Redirect configuration", "~/App_Plugins/MerchelloProviders/views/dialogs/PayTrace.providersettings.html")]	
	[GatewayProviderActivation(Constants.PayTraceRedirect.GatewayProviderKey, "PayTrace Redirect Payment Provider", "PayTrace Redirect Payment Provider")]	
	public class PayTraceRedirectPaymentGatewayProvider : PaymentGatewayProviderBase
	{
		#region AvailableResources

		/// <summary>
		/// The available resources.
		/// </summary>
		internal static readonly IEnumerable<IGatewayResource> AvailableResources =
			new List<IGatewayResource>()
				{
					new GatewayResource(Constants.PayTraceRedirect.PaymentCodes.RedirectCheckout, "PayTrace Redirect Checkout")
				};

		#endregion

		/// <summary>
		/// Initializes a new instance of the <see cref="PayTraceRedirectPaymentGatewayProvider"/> class.
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
		public PayTraceRedirectPaymentGatewayProvider(
			IGatewayProviderService gatewayProviderService,
			IGatewayProviderSettings gatewayProviderSettings,
			IRuntimeCacheProvider runtimeCacheProvider)
            : base(gatewayProviderService, gatewayProviderSettings, runtimeCacheProvider)
        {
		}

		/// <summary>
		/// Gets a list of all remaining payment methods.
		/// </summary>
		/// <returns>
		/// The <see cref="IEnumerable{IGatewayResource}"/>.
		/// </returns>
		public override IEnumerable<IGatewayResource> ListResourcesOffered()
		{
			// PaymentMethods is created in PaymentGatewayProviderBase.  It is a list of all previously saved payment methods
			return AvailableResources.Where(x => this.PaymentMethods.All(y => y.PaymentCode != x.ServiceCode));
		}

		/// <summary>
		/// Creates a <see cref="IPaymentGatewayMethod"/>
		/// </summary>
		/// <param name="gatewayResource">
		/// The <see cref="IGatewayResource"/> which contains the PaymentCode for the method
		/// </param>
		/// <param name="name">The name of the payment method</param>
		/// <param name="description">The description of the payment method</param>
		/// <returns>A <see cref="IPaymentGatewayMethod"/></returns>
		public override IPaymentGatewayMethod CreatePaymentMethod(
			IGatewayResource gatewayResource,
			string name,
			string description)
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
					case Constants.PayTraceRedirect.PaymentCodes.RedirectCheckout:
						return new PayTraceRedirectPaymentGatewayMethod(
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
		private IPayTraceRedirectService GetPayTraceApiService()
		{
			
			// S6 Extracted settings logic and placed it here rather than requiring to further modify an eComm core file (ProviderSettingsExtensions.cs)
			PayTraceRedirectProviderSettings settings;
			if (this.ExtendedData.ContainsKey(Constants.PayTraceRedirect.ExtendedDataKeys.ProviderSettings))
			{
				var json = this.ExtendedData.GetValue(Constants.PayTraceRedirect.ExtendedDataKeys.ProviderSettings);
				settings = JsonConvert.DeserializeObject<PayTraceRedirectProviderSettings>(json);
			}
			else
			{
				settings = new PayTraceRedirectProviderSettings();
			}

			return new PayTraceRedirectService(settings);
		}		
	}
}
