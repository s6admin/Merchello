
namespace Merchello.Web.Store.Models
{
	using Newtonsoft.Json;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;

	// Payment model to be used with the front-end PayTrace Encrypted Javascript library
	// This closely resembles the PayTraceCreditCardModel in Merchello.Providers which was based on the original PayTrace .NET API examples

	public class PayTraceEncryptedPaymentModel : StorePaymentModel
	{

		public string PayTraceToken { get; set; }

		/// <summary>
		/// Gets or sets the success redirect url.
		/// </summary>
		public string SuccessRedirectUrl { get; set; }

		[Required]
		[JsonProperty("amount")]
		public double Amount { get; set; }

		[Required]
		[JsonProperty("invoice_id")]
		public string OrderNumber { get; set; }

		[Required]
		[JsonProperty("credit_card")]
		public PayTraceCreditCard CreditCard { get; set; }

		// S6 Note that CSC is external to the CreditCard object data
		[Required]
		[JsonProperty("encrypted_csc")]
		public string CcCscEncrypted { get; set; }

		[Required]
		[JsonProperty("billing_address")]
		public PayTraceBillingAddress BillingAddress { get; set; }

		// TaxAmount is an OPTIONAL property to PayTrace, but we always want to ensure it is set in our orders
		[Required]
		[JsonProperty("tax_amount")]
		public decimal TaxAmount { get; set; }

		[JsonProperty("email")]
		public string CustomerEmail { get; set; }

		//[JsonProperty("submit_failed_payment_orders")]
		//public bool SubmitFailedPaymentOrders { get; set; }

		//// Permit a certain number of failed payment provider attempts before submitting the order without payment. Only applicable if SubmitFailedPaymentOrders is set to True		
		//private int maxFailedAttempts = 2;
		//[JsonProperty("max_failed_attempts")]
		//public int MaxFailedAttempts
		//{
		//	get { return maxFailedAttempts; }
		//	set { maxFailedAttempts = value; }
		//}

	}

	public class PayTraceCreditCard
	{
		[Required]
		[JsonProperty("encrypted_number")]
		public string CcNumberEncrypted { get; set; }
		
		[Required]
		[JsonProperty("expiration_month")]
		public string ExpireMonth { get; set; }

		[Required]
		[JsonProperty("expiration_year")]
		public string ExpireYear { get; set; }
	}

	public class PayTraceBillingAddress
	{
		[Required]
		[JsonProperty("name")]
		public string Name { get; set; }

		[Required]
		[JsonProperty("street_address")]
		public string StreetAddress { get; set; }

		[JsonProperty("street_address2")]
		public string StreetAddress2 { get; set; }

		[Required]
		[JsonProperty("city")]
		public string City { get; set; }

		[Required]
		[JsonProperty("state")]
		public string State { get; set; }

		[Required]
		[JsonProperty("zip")]
		public string Zip { get; set; }

		// 2 digit country code
		// http://en.wikipedia.org/wiki/ISO_3166-2
		[JsonProperty("country")]
		public string Country { get; set; }
	}
}
