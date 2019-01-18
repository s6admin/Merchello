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
			public const string GatewayProviderKey = "493b34d2-1a98-464d-9ee5-1a75f8d50353";
			
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
				/// Identifer for a front-end JSON PayTrace Encrypted payment.
				/// </summary>
				public const string Checkout = "PayTrace";

				/// <summary>
				/// Identifier for a PayTrace Redirect (remote site) payment
				/// </summary>
				public const string RedirectCheckout = "PayTraceRedirect";
			}

			/// <summary>
			/// PayTrace ExtendedData keys.
			/// </summary>
			public static class ExtendedDataKeys
			{
				/// <summary>
				/// Gets the processor settings.
				/// </summary>
				public const string ProviderSettings = "PayTraceprovidersettings";

				/// <summary>
				/// The pay pal  transaction key.
				/// </summary>
				public const string PayTraceTransaction = "PayTracetransaction";

				public const string PayTraceRedirectTransactionRecord = "PayTraceRedirectTransaction";

				/// <summary>
				/// A counter for keeping track of failed payment attempts. This is for website use only and is not an official PayTrace property.
				/// </summary>
				public const string FailedAttempts = "FailedPayTracePaymentAttempts";
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
						return "access_token";
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

				public static string PayTraceCreditCard
				{
					get
					{
						return "credit_card";
					}
				}

				public static string PayTraceBillingAddress
				{
					get
					{
						return "billing_address";
					}
				}
								
				public static string EncryptedCreditCardNumber
				{
					get
					{
						return "encrypted_number";
					}
				}

				public static string EncryptedCreditCardCode
				{
					get
					{
						return "encrypted_csc";
					}
				}

				public static string ExpirationMonth
				{
					get
					{
						return "expiration_month";
					}
				}

				public static string ExpirationYear
				{
					get
					{
						return "expiration_year";
					}
				}

				/// <summary>
				/// The Billing Address Name.
				/// </summary>
				/// <value>
				/// The name.
				/// </value>
				public static string BillingAddressName
				{
					get
					{
						return "name";
					}
				}

				public static string StreetAddress
				{
					get
					{
						return "street_address";
					}
				}

				public static string StreetAddress2
				{
					get
					{
						return "street_address2";
					}
				}

				public static string City
				{
					get
					{
						return "city";
					}
				}

				public static string State
				{
					get
					{
						return "state";
					}
				}

				public static string Zip
				{
					get
					{
						return "zip";
					}
				}

				public static string Country
				{
					get
					{
						return "country";
					}
				}

				/// <summary>
				/// The TOTAL transaction Amount, including any taxes, shipping costs, and/or additional fees.
				/// </summary>
				/// <value>
				/// The amount.
				/// </value>
				public static string Amount
				{
					get
					{
						return "amount";
					}
				}

				public static string TaxAmount
				{
					get {
						return "tax_amount";
					}

				}
			}

			public static class ResponseKeys
			{

				public const string ResponseCode = "response_code";
				public const string TransactionId = "transaction_id";
				public const string ApprovalCode = "approval_code";
				public const string ApprovalMessage = "approval_message";
				public const string AvsResponse = "avs_response";
				public const string CscResponse = "csc_response";
				public const string ExternalTransactionid = "external_transaction_id";
				public const string MaskedCardNumber = "masked_card_number";
			}
		}
	}
}
