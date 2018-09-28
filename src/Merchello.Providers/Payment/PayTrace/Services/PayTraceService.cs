namespace Merchello.Providers.Payment.PayTrace.Services
{
    using System;
    using System.Collections.Generic;

    using Merchello.Core.Events;
    using Merchello.Core.Logging;
    using Merchello.Core.Models;
    using Merchello.Providers.Exceptions;
    using Merchello.Providers.Payment.PayTrace.Factories;
    using Merchello.Providers.Payment.PayTrace.Models;

    using global::PayTrace.PayTraceAPIInterfaceService;

    using global::PayTrace.PayTraceAPIInterfaceService.Model;

    using Umbraco.Core;
    using Umbraco.Core.Events;

    /// <summary>
    /// Represents a PayTraceCheckoutService.
    /// </summary>
    public class PayTraceCheckoutService : PayTraceApiServiceBase, IPayTraceCheckoutService
    {
        /// <summary>
        /// The default return URL.
        /// </summary>
        private const string DefaultReturnUrl = "{0}/umbraco/merchello/PayTrace/success?invoiceKey={1}&paymentKey={2}";

        /// <summary>
        /// The default cancel URL.
        /// </summary>
        private const string DefaultCancelUrl = "{0}/umbraco/merchello/PayTrace/success?invoiceKey={1}&paymentKey={2}";

        /// <summary>
        /// The version.
        /// </summary>
        private const string Version = "104.0";

        /// <summary>
        /// The Website URL.
        /// </summary>
        private readonly string _websiteUrl;

        /// <summary>
        /// The <see cref="CheckoutResponseFactory"/>.
        /// </summary>
        private readonly CheckoutResponseFactory _responseFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="PayTraceCheckoutService"/> class.
        /// </summary>
        /// <param name="settings">
        /// The settings.
        /// </param>
        public PayTraceCheckoutService(PayTraceProviderSettings settings)
            : base(settings)
        {
            _websiteUrl = PayTraceApiHelper.GetBaseWebsiteUrl();
            _responseFactory = new CheckoutResponseFactory();
        }

        /// <summary>
        /// Occurs before PayTraceCheckoutService is completely initialized.
        /// </summary>
        /// <remarks>
        /// Allows for overriding defaults
        /// </remarks>
        public static event TypedEventHandler<PayTraceCheckoutService, ObjectEventArgs<PayTraceCheckoutUrls>> InitializingCheckout;

        /// <summary>
        /// Occurs before adding the PayTrace  Checkout details to the request
        /// </summary>
        public static event TypedEventHandler<IPayTraceCheckoutService, ObjectEventArgs<PayTraceCheckoutRequestDetailsOverride>> SettingCheckoutRequestDetails;

        ///// <summary>
        ///// Occurs before adding the PayTrace  Checkout Details to the DoCheckout.
        ///// </summary>
        //public static event TypedEventHandler<IPayTraceApiPaymentService, ObjectEventArgs<PayTraceCheckoutRequestDetailsOverride>> SettingDoCheckoutRequestDetails;

        /// <summary>
        /// Performs the setup for an  checkout.
        /// </summary>
        /// <param name="invoice">
        /// The <see cref="IInvoice"/>.
        /// </param>
        /// <param name="payment">
        /// The <see cref="IPayment"/>
        /// </param>
        /// <returns>
        /// The <see cref="SetCheckoutResponseType"/>.
        /// </returns>
        public PayTraceTransactionRecord SetCheckout(IInvoice invoice, IPayment payment)
        {
            var urls = new PayTraceCheckoutUrls
                {
                    ReturnUrl = string.Format(DefaultReturnUrl, _websiteUrl, invoice.Key, payment.Key),
                    CancelUrl = string.Format(DefaultCancelUrl, _websiteUrl, invoice.Key, payment.Key)
                };

            // Raise the event so that the urls can be overridden
            InitializingCheckout.RaiseEvent(new ObjectEventArgs<PayTraceCheckoutUrls>(urls), this);
            return SetCheckout(invoice, payment, urls.ReturnUrl, urls.CancelUrl);
        }


        /// <summary>
        /// The capture success.
        /// </summary>
        /// <param name="invoice">
        /// The invoice.
        /// </param>
        /// <param name="payment">
        /// The payment.
        /// </param>
        /// <param name="amount">
        /// The amount.
        /// </param>
        /// <param name="isPartialPayment">
        /// The is partial payment.
        /// </param>
        /// <returns>
        /// The <see cref="CheckoutResponse"/>.
        /// </returns>
        public PayTraceTransactionRecord Capture(IInvoice invoice, IPayment payment, decimal amount, bool isPartialPayment)
        {
            // Get the transaction record
            var record = payment.GetPayTraceTransactionRecord();

            CheckoutResponse result = null;

            try
            {
                var amountFactory = new PayTraceBasicAmountTypeFactory(PayTraceApiHelper.GetPayTraceCurrencyCode(invoice.CurrencyCode));

                var authorizationId = record.Data.AuthorizationTransactionId;

                // do  checkout
                var request = new DoCaptureRequestType()
                {
                    AuthorizationID = authorizationId,
                    Amount = amountFactory.Build(amount),
                    CompleteType = isPartialPayment ? CompleteCodeType.NOTCOMPLETE : CompleteCodeType.COMPLETE
                };

                var doCaptureReq = new DoCaptureReq() { DoCaptureRequest = request };

                var service = GetPayTraceService();
                var doCaptureResponse = service.DoCapture(doCaptureReq);
                result = _responseFactory.Build(doCaptureResponse, record.Data.Token);

                if (result.Success())
                {
                    var transactionId = doCaptureResponse.DoCaptureResponseDetails.PaymentInfo.TransactionID;
                    record.Data.CaptureTransactionId = transactionId;
                }
            }
            catch (Exception ex)
            {
                result = _responseFactory.Build(ex);
            }

            record.DoCapture = result;
            record.Success = result.Success();
            return record;
        }

        /// <summary>
        /// Refunds or partially refunds a payment.
        /// </summary>
        /// <param name="invoice">
        /// The invoice.
        /// </param>
        /// <param name="payment">
        /// The payment.
        /// </param>
        /// <param name="amount">
        /// The amount of the refund.
        /// </param>
        /// <returns>
        /// The <see cref="PayTraceTransactionRecord"/>.
        /// </returns>
        public CheckoutResponse Refund(IInvoice invoice, IPayment payment, decimal amount)
        {
            var record = payment.GetPayTraceTransactionRecord();

            // Ensure the currency code
            if (record.Data.CurrencyCode.IsNullOrWhiteSpace())
            {
                var ex = new PayTraceApiException("CurrencyCode was not found in payment extended data PayTrace transaction data record.  Cannot perform refund.");
                return _responseFactory.Build(ex);
            }

            // Ensure the transaction id
            if (record.Data.CaptureTransactionId.IsNullOrWhiteSpace())
            {
                var ex = new PayTraceApiException("CaptureTransactionId was not found in payment extended data PayTrace transaction data record.  Cannot perform refund.");
                return _responseFactory.Build(ex);
            }

            // Get the decimal configuration for the current currency
            var currencyCodeType = PayTraceApiHelper.GetPayTraceCurrencyCode(record.Data.CurrencyCode);
            var basicAmountFactory = new PayTraceBasicAmountTypeFactory(currencyCodeType);

            CheckoutResponse result = null;

            if (amount > payment.Amount) amount = payment.Amount;

            try
            {
                var request = new RefundTransactionRequestType
                    {
                        InvoiceID = invoice.PrefixedInvoiceNumber(),
                        PayerID = record.Data.PayerId,
                        RefundSource = RefundSourceCodeType.DEFAULT,
                        Version = record.DoCapture.Version,
                        TransactionID = record.Data.CaptureTransactionId,
                        Amount = basicAmountFactory.Build(amount)
                    };

                var wrapper = new RefundTransactionReq { RefundTransactionRequest = request };

                var refundTransactionResponse = GetPayTraceService().RefundTransaction(wrapper);

                result = _responseFactory.Build(refundTransactionResponse, record.Data.Token);
            }
            catch (Exception ex)
            {
                result = _responseFactory.Build(ex);
            }

            return result;
        }

        /// <summary>
        /// Performs the GetCheckoutDetails operation.
        /// </summary>
        /// <param name="payment">
        /// The payment.
        /// </param>
        /// <param name="token">
        /// The token.
        /// </param>
        /// <param name="record">
        /// The record.
        /// </param>
        /// <returns>
        /// The <see cref="PayTraceTransactionRecord"/>.
        /// </returns>
        internal PayTraceTransactionRecord GetCheckoutDetails(IPayment payment, string token, PayTraceTransactionRecord record)
        {
            record.Success = true;
            CheckoutResponse result = null;
            try
            {
                var getDetailsRequest = new GetCheckoutDetailsReq
                {
                    GetCheckoutDetailsRequest = new GetCheckoutDetailsRequestType(token)
                };

                var service = GetPayTraceService();
                var response = service.GetCheckoutDetails(getDetailsRequest);
                result = _responseFactory.Build(response, token);

                if (result.Success())
                {
                    record.Data.PayerId = response.GetCheckoutDetailsResponseDetails.PayerInfo.PayerID;
                }
            }
            catch (Exception ex)
            {
                result = _responseFactory.Build(ex);
            }

            record.GetCheckoutDetails = result;
            record.Success = result.Success();
            return record;
        }

        /// <summary>
        /// The do  checkout payment.
        /// </summary>
        /// <param name="invoice">
        /// The invoice.
        /// </param>
        /// <param name="payment">
        /// The payment.
        /// </param>
        /// <param name="token">
        /// The token.
        /// </param>
        /// <param name="payerId">
        /// The payer id.
        /// </param>
        /// <param name="record">
        /// The record of the transaction.
        /// </param>
        /// <returns>
        /// The <see cref="PayTraceTransactionRecord"/>.
        /// </returns>
        internal PayTraceTransactionRecord DoCheckoutPayment(IInvoice invoice, IPayment payment, string token, string payerId, PayTraceTransactionRecord record)
        {
            var factory = new PayTracePaymentDetailsTypeFactory();

            CheckoutResponse result = null;
            try
            {
                // do  checkout
                var request = new DoCheckoutPaymentRequestType(
                        new DoCheckoutPaymentRequestDetailsType
                        {
                            Token = token,
                            PayerID = payerId,
                            PaymentDetails =
                                    new List<PaymentDetailsType>
                                        {
                                            factory.Build(invoice, PaymentActionCodeType.ORDER)
                                        }
                        });

                var doCheckoutPayment = new DoCheckoutPaymentReq
                {
                    DoCheckoutPaymentRequest = request
                };

                var service = GetPayTraceService();
                var response = service.DoCheckoutPayment(doCheckoutPayment);
                result = _responseFactory.Build(response, token);

                var transactionId = response.DoCheckoutPaymentResponseDetails.PaymentInfo[0].TransactionID;
                var currency = response.DoCheckoutPaymentResponseDetails.PaymentInfo[0].GrossAmount.currencyID;
                var amount = response.DoCheckoutPaymentResponseDetails.PaymentInfo[0].GrossAmount.value;

                record.Data.CheckoutPaymentTransactionId = transactionId;
                record.Data.CurrencyId = currency.ToString();
                record.Data.AuthorizedAmount = amount;

            }
            catch (Exception ex)
            {
                result = _responseFactory.Build(ex);
            }

            record.DoCheckoutPayment = result;
            record.Success = result.Success();

            return record;
        }

        /// <summary>
        /// Validates a successful response.
        /// </summary>
        /// <param name="invoice">
        /// The invoice.
        /// </param>
        /// <param name="payment">
        /// The payment.
        /// </param>
        /// <param name="token">
        /// The token.
        /// </param>
        /// <param name="payerId">
        /// The payer id.
        /// </param>
        /// <param name="record">
        /// The record.
        /// </param>
        /// <returns>
        /// The <see cref="CheckoutResponse"/>.
        /// </returns>
        /// <remarks>
        /// PayTrace returns to the success URL even if the payment was declined. e.g.  The success URL represents a successful transaction
        /// not a successful payment so we need to do another request to verify the payment was completed and get additional information
        /// such as the transaction id so we can do refunds etc.
        /// </remarks>
        internal PayTraceTransactionRecord Authorize(IInvoice invoice, IPayment payment, string token, string payerId, PayTraceTransactionRecord record)
        {
            // Now we have to get the transaction details for the successful payment
            CheckoutResponse result = null;
            try
            {
                // do authorization
                var service = GetPayTraceService();
                var doAuthorizationResponse = service.DoAuthorization(new DoAuthorizationReq
                {
                    DoAuthorizationRequest = new DoAuthorizationRequestType
                    {
                        TransactionID = record.Data.CheckoutPaymentTransactionId,
                        Amount = new BasicAmountType(PayTraceApiHelper.GetPayTraceCurrencyCode(record.Data.CurrencyId), record.Data.AuthorizedAmount)
                    }
                });

                result = _responseFactory.Build(doAuthorizationResponse, token);
                if (result.Success())
                {
                    record.Data.Authorized = true;
                    record.Data.AuthorizationTransactionId = doAuthorizationResponse.TransactionID;
                }
            }
            catch (Exception ex)
            {
                result = _responseFactory.Build(ex);
            }

            record.DoAuthorization = result;

            return record;
        }



        /// <summary>
        /// Performs the setup for an  checkout.
        /// </summary>
        /// <param name="invoice">
        /// The <see cref="IInvoice"/>.
        /// </param>
        /// <param name="payment">
        /// The <see cref="IPayment"/>
        /// </param>
        /// <param name="returnUrl">
        /// The return URL.
        /// </param>
        /// <param name="cancelUrl">
        /// The cancel URL.
        /// </param>
        /// <returns>
        /// The <see cref="CheckoutResponse"/>.
        /// </returns>
        protected virtual PayTraceTransactionRecord SetCheckout(IInvoice invoice, IPayment payment, string returnUrl, string cancelUrl)
        {
            var record = new PayTraceTransactionRecord
                             {
                                 Success = true,
                                 Data = { Authorized = false, CurrencyCode = invoice.CurrencyCode }
                             };

            var factory = new PayTracePaymentDetailsTypeFactory(new PayTraceFactorySettings { WebsiteUrl = _websiteUrl });
            var paymentDetailsType = factory.Build(invoice, PaymentActionCodeType.ORDER);

            // The API requires this be in a list
            var paymentDetailsList = new List<PaymentDetailsType>() { paymentDetailsType };

            // Checkout details
            var ecDetails = new SetCheckoutRequestDetailsType()
                    {
                        ReturnURL = returnUrl,
                        CancelURL = cancelUrl,
                        PaymentDetails = paymentDetailsList,
                        AddressOverride = "1"
                    };

            // Trigger the event to allow for overriding ecDetails
            var ecdOverride = new PayTraceCheckoutRequestDetailsOverride(invoice, payment, ecDetails);
            SettingCheckoutRequestDetails.RaiseEvent(new ObjectEventArgs<PayTraceCheckoutRequestDetailsOverride>(ecdOverride), this);

            // The CheckoutRequest
            var request = new SetCheckoutRequestType
                    {
                        Version = Version,
                        SetCheckoutRequestDetails = ecdOverride.CheckoutDetails
                    };

            // Crete the wrapper for  Checkout
            var wrapper = new SetCheckoutReq
                              {
                                  SetCheckoutRequest = request
                              };

            try
            {
                var service = GetPayTraceService();
                var response = service.SetCheckout(wrapper);

                record.SetCheckout = _responseFactory.Build(response, response.Token);
                if (record.SetCheckout.Success())
                {
                    record.Data.Token = response.Token;
                    record.SetCheckout.RedirectUrl = GetRedirectUrl(response.Token);
                }
                else
                {
                    foreach (var et in record.SetCheckout.ErrorTypes)
                    {
                        var code = et.ErrorCode;
                        var sm = et.ShortMessage;
                        var lm = et.LongMessage;
                        MultiLogHelper.Warn<PayTraceCheckoutService>(string.Format("{0} {1} {2}", code, lm, sm));
                    }

                    record.Success = false;
                }
            }
            catch (Exception ex)
            {
                record.Success = false;
                record.SetCheckout = _responseFactory.Build(ex);
            }

            return record;
        }
        
        /// <summary>
        /// Gets the <see cref="PayTraceAPIInterfaceServiceService"/>.
        /// </summary>
        /// <returns>
        /// The <see cref="PayTraceAPIInterfaceServiceService"/>.
        /// </returns>
        /// <exception cref="PayTraceApiException">
        /// Throws an exception if 
        /// </exception>
        protected virtual PayTraceAPIInterfaceServiceService GetPayTraceService()
        {
            // We are getting the SDK authentication values from the settings saved through the back office
            // and stored in ExtendedData.  This returns an Attempt<PayTraceSettings> so that we can 
            // assert that the values have been entered into the back office.
            var attemptSdk = Settings.GetCheckoutSdkConfig();
            if (!attemptSdk.Success) throw attemptSdk.Exception;

            EnsureSslTslChannel();
            return new PayTraceAPIInterfaceServiceService(attemptSdk.Result);
        }

        /// <summary>
        /// Gets the redirection URL for PayTrace.
        /// </summary>
        /// <param name="token">
        /// The token.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        /// <remarks>
        /// This value will be put into the <see cref="IPayment"/>'s <see cref="ExtendedDataCollection"/>
        /// </remarks>
        private string GetRedirectUrl(string token)
        {
            return string.Format("https://www.{0}PayTrace.com/cgi-bin/webscr?cmd=_-checkout&token={1}", Settings.Mode == PayTraceMode.Live ? string.Empty : "sandbox.", token);
        }
    }
}