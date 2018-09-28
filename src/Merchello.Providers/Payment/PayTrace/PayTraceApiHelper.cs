namespace Merchello.Providers.Payment.PayTrace
{
    using System;
    using System.Web;

    using global::PayTrace.PayTraceAPIInterfaceService.Model;

    using Merchello.Core;
    using Merchello.Core.Logging;
    using Merchello.Core.Models;
    using Merchello.Providers.Models;
    using Merchello.Providers.Payment.Braintree.Controllers;
    using Merchello.Providers.Payment.PayTrace.Factories;
    using Merchello.Providers.Payment.PayTrace.Models;
    using Merchello.Providers.Payment.PayTrace.Provider;

    using Constants = Merchello.Providers.Constants;

    /// <summary>
    /// Utility class that assists in PayTrace API calls.
    /// </summary>
    public class PayTraceApiHelper
    {
        /// <summary>
        /// Maps <see cref="ICurrency"/> to <see cref="CurrencyCodeType"/>.
        /// </summary>
        /// <param name="currencyCode">
        /// The currency code.
        /// </param>
        /// <returns>
        /// The <see cref="CurrencyCodeType"/>.
        /// </returns>
        public static CurrencyCodeType GetPayTraceCurrencyCode(string currencyCode)
        {
            try
            {
                return (CurrencyCodeType)Enum.Parse(typeof(CurrencyCodeType), currencyCode, true);
            }
            catch (Exception ex)
            {
                var logData = MultiLogger.GetBaseLoggingData();
                logData.AddCategory("PayTrace");

                MultiLogHelper.WarnWithException<PayTraceBasicAmountTypeFactory>("Failed to map currency code", ex, logData);

                throw;
            }
        }

        /// <summary>
        /// Gets the <see cref="PayTraceProviderSettings"/>.
        /// </summary>
        /// <returns>
        /// The <see cref="PayTraceProviderSettings"/>.
        /// </returns>
        /// <exception cref="NullReferenceException">
        /// Throws a null reference exception if the PayTraceGatewayProvider has not been activated
        /// </exception>
        public static PayTraceProviderSettings GetPayTraceProviderSettings()
        {
            var provider = (PayTracePaymentGatewayProvider)MerchelloContext.Current.Gateways.Payment.GetProviderByKey(Constants.PayTrace.GatewayProviderSettingsKey);

            if (provider != null) return provider.ExtendedData.GetPayTraceProviderSettings();

            var logData = MultiLogger.GetBaseLoggingData();
            logData.AddCategory("GatewayProviders");
            logData.AddCategory("PayTrace");

            var ex = new NullReferenceException("The PayTracePaymentGatewayProvider could not be resolved.  The provider must be activiated");
            MultiLogHelper.Error<BraintreeApiController>("PayTracePaymentGatewayProvider not activated.", ex, logData);
            throw ex;
        }

        /// <summary>
        /// Gets the base website url for constructing PayTrace response URLs.
        /// </summary>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public static string GetBaseWebsiteUrl()
        {
            var websiteUrl = string.Empty;
            try
            {
                var url = HttpContext.Current.Request.Url;
                websiteUrl =
                    string.Format(
                        "{0}://{1}{2}",
                        url.Scheme,
                        url.Host,
                        url.IsDefaultPort ? string.Empty : ":" + url.Port).EnsureNotEndsWith('/');
            }
            catch (Exception ex)
            {
                var logData = MultiLogger.GetBaseLoggingData();
                logData.AddCategory("PayTrace");

                MultiLogHelper.WarnWithException(
                    typeof(PayTraceApiHelper),
                    "Failed to initialize factory setting for WebsiteUrl.  HttpContext.Current.Request is likely null.",
                    ex,
                    logData);
            }

            return websiteUrl;
        }
    }
}