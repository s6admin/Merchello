namespace Merchello.Providers
{
    using System;

    /// <summary>
    /// Constants segment for the PayTrace provider.
    /// </summary>
    public static partial class Constants
    {
        /// <summary>
        /// PayTrace constants.
        /// </summary>
        public static class PayTrace
        {
            /// <summary>
            /// The PayTrace payment gateway provider key.
            /// </summary>
            /// <remarks>            
            /// </remarks>
            public const string GatewayProviderKey = "TODO";


            /// <summary>
            /// Gets the gateway provider settings key.
            /// </summary>
            public static Guid GatewayProviderSettingsKey
            {
                get
                {
                    return new Guid(GatewayProviderKey);
                }
            }

            /// <summary>
            /// Payment codes for the PayTrace provider.
            /// </summary>
            public static class PaymentCodes
            {
                /// <summary>
                /// Gets the Checkout Payment code.
                /// </summary>
                public const string Checkout = "PayTrace";
            }

            /// <summary>
            /// PayTrace ExtendedData keys.
            /// </summary>
            public static class ExtendedDataKeys
            {
                /// <summary>
                /// Gets the processor settings.
                /// </summary>
                public const string ProviderSettings = "paytraceprovidersettings";

                /// <summary>
                /// The PayTrace transaction key.
                /// </summary>
                public const string PayTraceTransaction = "paytracetransaction";
            }

            /// <summary>
            /// The processor arguments keys.
            /// </summary>
            public static class ProcessorArgumentsKeys
            {
                /// <summary>
                /// Gets the return URL.
                /// </summary>
                public static string ReturnUrl
                {
                    get
                    {
                        return "ReturnUrl";
                    }
                }

                /// <summary>
                /// Gets the cancel url.
                /// </summary>
                public static string CancelUrl
                {
                    get
                    {
                        return "CancelUrl";
                    }
                }

                /// <summary>
                /// Gets the product content slug.
                /// </summary>
                public static string ProductContentSlug
                {
                    get
                    {
                        return "productContentSlug";
                    }
                }

                /// <summary>
                /// Gets the internal token key.
                /// </summary>
                internal static string InternalTokenKey
                {
                    get
                    {
                        return "internalToken";
                    }
                }

                /// <summary>
                /// Gets the internal payer id key.
                /// </summary>
                internal static string InternalPayerIDKey
                {
                    get
                    {
                        return "internalPayerID";
                    }
                }

                /// <summary>
                /// Gets the internal payment key key.
                /// </summary>
                internal static string InternalPaymentKeyKey
                {
                    get
                    {
                        return "internalPaymentKey";
                    }
                } 

            }
        }
    }
}
