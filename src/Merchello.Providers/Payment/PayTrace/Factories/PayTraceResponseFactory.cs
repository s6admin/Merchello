namespace Merchello.Providers.Payment.PayTrace.Factories
{
    using System;
    using System.Collections.Generic;

    using Merchello.Core.Logging;
    using Merchello.Providers.Payment.PayTrace.Models;
    using Merchello.Providers.Payment.PayTrace.Services;

    //using global::PayTrace.PayTraceAPIInterfaceService.Model;

    /// <summary>
    /// A factory responsible for building <see cref="PayTraceTransaction"/>.
    /// </summary>
    internal class CheckoutResponseFactory
    {
        /// <summary>
        /// Gets the <see cref="CheckoutResponse"/> from PayTrace's <see cref="AbstractResponseType"/>.
        /// </summary>
        /// <param name="response">
        /// The response.
        /// </param>
        /// <param name="token">
        /// The token.
        /// </param>
        /// <returns>
        /// The <see cref="CheckoutResponse"/>.
        /// </returns>
        public CheckoutResponse Build(AbstractResponseType response, string token = "")
        {
            return new CheckoutResponse
            {
                Ack = response.Ack,
                Build = response.Build,
                ErrorTypes = response.Errors,
                Token = token,
                Version = response.Version
            };
        }

        /// <summary>
        /// Constructs an error response message from an exception.
        /// </summary>
        /// <param name="ex">
        /// The ex.
        /// </param>
        /// <returns>
        /// The <see cref="PayTraceResponse"/>.
        /// </returns>
        public PayTraceResponse Build(Exception ex)
        {
            var logData = MultiLogger.GetBaseLoggingData();
            logData.AddCategory("GatewayProviders");
            logData.AddCategory("PayTrace");

            // bubble up the error
            var errorType = new ErrorType()
            {
                SeverityCode = SeverityCodeType.CUSTOMCODE,
                ShortMessage = ex.Message,
                LongMessage = ex.Message,
                ErrorCode = "PPEService"
            };

            logData.SetValue("PayTraceErrorType", errorType);

            MultiLogHelper.Error<PayTraceCheckoutService>("Failed to get response from PayTraceAPIInterfaceServiceService", ex, logData);

            return new PayTraceResponse { Ack = AckCodeType.CUSTOMCODE, ErrorTypes = new List<ErrorType> { errorType } };
        }

    }
}