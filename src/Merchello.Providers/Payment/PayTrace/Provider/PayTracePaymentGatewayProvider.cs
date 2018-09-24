using System;
using System.Collections.Generic;
using System.Linq;
using Merchello.Core.Gateways;
using Merchello.Core.Gateways.Payment;
using Merchello.Core.Models;
using Merchello.Core.Services;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;

namespace Merchello.Providers.Payment.PayTrace.Provider
{
	[GatewayProviderActivation("493B34D2-1A98-464D-9EE5-1A75F8D50353", "PayTrace Payment Provider", "PayTrace Payment Provider")]
	public class PayTracePaymentGatewayProvider : PaymentGatewayProviderBase, IPayTracePaymentGatewayProvider
	{
		#region AvailableResources

		/// <summary>
		/// The available resources.
		/// </summary>
		internal static readonly IEnumerable<IGatewayResource> AvailableResources = new List<IGatewayResource>
		{
			//new GatewayResource("PurchaseOrder", "Purchase Order")
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
			var paymentCode = gatewayResource.ServiceCode + "-" + Guid.NewGuid();

			var attempt = GatewayProviderService.CreatePaymentMethodWithKey(GatewayProviderSettings.Key, name, description, paymentCode);

			if (attempt.Success)
			{
				PaymentMethods = null;

				return new PayTracePaymentGatewayMethod(GatewayProviderService, attempt.Result);
			}

			LogHelper.Error<PayTracePaymentGatewayProvider>(string.Format("Failed to create a payment method name: {0}, description {1}, paymentCode {2}", name, description, paymentCode), attempt.Exception);

			throw attempt.Exception;
		}

		/// <summary>
		/// Gets a <see cref="IPaymentGatewayMethod"/> by it's unique 'key'
		/// </summary>
		/// <param name="paymentMethodKey">The key of the <see cref="IPaymentMethod"/></param>
		/// <returns>A <see cref="IPaymentGatewayMethod"/></returns>
		public override IPaymentGatewayMethod GetPaymentGatewayMethodByKey(Guid paymentMethodKey)
		{
			var paymentMethod = PaymentMethods.FirstOrDefault(x => x.Key == paymentMethodKey);

			if (paymentMethod == null) throw new NullReferenceException("PaymentMethod not found");

			return new PayTracePaymentGatewayMethod(GatewayProviderService, paymentMethod);
		}

		/// <summary>
		/// Gets a <see cref="IPaymentGatewayMethod"/> by it's payment code
		/// </summary>
		/// <param name="paymentCode">The payment code of the <see cref="IPaymentGatewayMethod"/></param>
		/// <returns>A <see cref="IPaymentGatewayMethod"/></returns>
		public override IPaymentGatewayMethod GetPaymentGatewayMethodByPaymentCode(string paymentCode)
		{
			var paymentMethod = PaymentMethods.FirstOrDefault(x => x.PaymentCode == paymentCode);

			if (paymentMethod == null) throw new NullReferenceException("PaymentMethod not found");

			return new PayTracePaymentGatewayMethod(GatewayProviderService, paymentMethod);
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
