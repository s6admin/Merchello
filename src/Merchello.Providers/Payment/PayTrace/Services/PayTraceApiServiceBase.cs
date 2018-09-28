namespace Merchello.Providers.Payment.PayTrace.Services
{
    using System;
    using System.Net;

    using Merchello.Core.Logging;
    using Merchello.Providers.Payment.PayTrace.Models;

    using global::PayTrace;

    using Umbraco.Core;

    /// <summary>
    /// A base class of <see cref="IPayTraceApiServiceBase"/>s.
    /// </summary>
    public class PayTraceApiServiceBase : IPayTraceApiServiceBase
    {
        /// <summary>
        /// The <see cref="PayTraceProviderSettings"/>.
        /// </summary>
        private readonly PayTraceProviderSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="PayTraceApiServiceBase"/> class.
        /// </summary>
        /// <param name="settings">
        /// The settings.
        /// </param>
        protected PayTraceApiServiceBase(PayTraceProviderSettings settings)
        {
            Mandate.ParameterNotNull(settings, "settings");
            _settings = settings;
        }

        /// <summary>
        /// Gets the settings.
        /// </summary>
        internal PayTraceProviderSettings Settings
        {
            get
            {
                return _settings;
            }
        }

        /// <summary>
        /// Ensures the connection channel to PayTrace.
        /// </summary>
        protected static void EnsureSslTslChannel()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.DefaultConnectionLimit = 9999;
        }

        /// <summary>
        /// Gets the access token.
        /// </summary>
        /// <returns>
        /// The access token.
        /// </returns>
        protected APIContext GetApiContext()
        {
            try
            {
                EnsureSslTslChannel();

                var attempt = _settings.GetApiSdkConfig();

                if (!attempt.Success) throw attempt.Exception;

                var accessToken = new OAuthTokenCredential(_settings.ClientId, _settings.ClientSecret, attempt.Result).GetAccessToken();

                return new APIContext(accessToken);
            }
            catch (Exception ex)
            {
                var logData = GetLoggerData();
                MultiLogHelper.Error<PayTraceApiServiceBase>("Failed to create PayTrace APIContext", ex, logData);
                throw;
            }

        }

        /// <summary>
        /// Gets the extended logger data.
        /// </summary>
        /// <returns>
        /// The <see cref="IExtendedLoggerData"/>.
        /// </returns>
        protected IExtendedLoggerData GetLoggerData()
        {
            var logData = MultiLogger.GetBaseLoggingData();

            logData.AddCategory("PayTrace");

            return logData;
        }
    }
}