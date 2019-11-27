using Merchello.Core.Gateways.Taxation.AvaTax;
using Merchello.Core.Models;
using Merchello.Core.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;

namespace Merchello.Core.Gateways.Taxation.AvaTax
{
	/// <summary>
	/// AvaTax Gateway Provider
	/// </summary>
	/// <seealso cref="Merchello.Core.Gateways.Taxation.TaxationGatewayProviderBase" />
	/// <seealso cref="Merchello.Core.Gateways.Taxation.AvaTax.IAvaTaxGatewayProvider" />
	[GatewayProviderActivation("ab977f46-0121-49f9-b7d4-15c5e39df14f", "AvaTax Provider", "AvaTax Provider")]
	public class AvaTaxGatewayProvider : TaxationGatewayProviderBase, IAvaTaxGatewayProvider
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AvaTaxGatewayProvider"/> class.
		/// </summary>
		/// <param name="gatewayProviderService">The gateway provider service.</param>
		/// <param name="gatewayProviderSettings">The gateway provider settings.</param>
		/// <param name="runtimeCacheProvider">The runtime cache provider.</param>
		public AvaTaxGatewayProvider(IGatewayProviderService gatewayProviderService,
			IGatewayProviderSettings gatewayProviderSettings,
			IRuntimeCacheProvider runtimeCacheProvider)
			: base(gatewayProviderService, gatewayProviderSettings, runtimeCacheProvider)
		{
		}

		/// <summary>
		/// Creates a <see cref="ITaxationGatewayMethod" />
		/// </summary>
		/// <param name="countryCode">The two letter ISO Country Code</param>
		/// <param name="taxPercentageRate">The decimal percentage tax rate</param>
		/// <returns>
		/// The <see cref="ITaxationGatewayMethod" />
		/// </returns>
		public override ITaxationGatewayMethod CreateTaxMethod(string countryCode, decimal taxPercentageRate)
		{
			var attempt = ListResourcesOffered().FirstOrDefault(x => x.ServiceCode.Equals(countryCode)) != null
			  ? GatewayProviderService.CreateTaxMethodWithKey(GatewayProviderSettings.Key, countryCode, taxPercentageRate)
			  : Attempt<ITaxMethod>.Fail(new ConstraintException("AvaTax method has already been defined for " + countryCode));


			if (attempt.Success)
			{
				return new AvaTaxGatewayMethod(attempt.Result);
			}

			LogHelper.Error<TaxationGatewayProviderBase>("CreateTaxMethod failed.", attempt.Exception);

			throw attempt.Exception;
		}

		/// <summary>
		/// Gets a <see cref="ITaxationGatewayMethod" /> by it's unique 'key' (GUID)
		/// </summary>
		/// <param name="countryCode">The two char ISO country code</param>
		/// <returns>
		/// The <see cref="ITaxationGatewayMethod" />
		/// </returns>
		public override ITaxationGatewayMethod GetGatewayTaxMethodByCountryCode(string countryCode)
		{
			var taxMethod = this.FindTaxMethodForCountryCode(countryCode);

			return taxMethod != null ? new AvaTaxGatewayMethod(taxMethod) : null;
		}

		/// <summary>
		/// Gets the taxation by product method.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <returns></returns>
		public ITaxationByProductMethod GetTaxationByProductMethod(Guid key)
		{
			var taxMethod = TaxMethods.FirstOrDefault(x => x.Key == key);
			return taxMethod != null ? new AvaTaxGatewayMethod(taxMethod) : null;
		}

		/// <summary>
		/// Gets a collection of all <see cref="ITaxationGatewayMethod" /> associated with this provider
		/// </summary>
		/// <returns>
		/// A collection of <see cref="ITaxationGatewayMethod" />
		/// </returns>
		public override IEnumerable<ITaxationGatewayMethod> GetAllGatewayTaxMethods()
		{
			return TaxMethods.Select(taxMethod => new AvaTaxGatewayMethod(taxMethod));
		}

		/// <summary>
		/// Returns a collection of all possible gateway methods associated with this provider
		/// </summary>
		/// <returns>
		/// A collection of <see cref="IGatewayResource" />
		/// </returns>
		public override IEnumerable<IGatewayResource> ListResourcesOffered()
		{
			var countryCodes = GatewayProviderService.GetAllShipCountries().Select(x => x.CountryCode).Distinct();

			var resources =
				countryCodes.Select(x => new GatewayResource(x, x + "-AvaTax"))
					.Where(code => TaxMethods.FirstOrDefault(x => x.CountryCode.Equals(code.ServiceCode)) == null);

			return resources;
		}
	}
}
