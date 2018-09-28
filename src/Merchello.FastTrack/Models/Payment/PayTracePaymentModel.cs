
namespace Merchello.FastTrack.Models.Payment
{
	using Newtonsoft.Json;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;

	// S6 Demo for standard purchase order payment provider, not used with redirect PayTrace

	public class PayTracePaymentModel : FastTrackPaymentModel
	{
				
		[Required]
		[JsonProperty("amount")]
		public double amount { get; set; }
				
		[Required]
		[JsonProperty("invoice_id")]
		public string OrderNumber { get; set; }

		[Required]
		[JsonProperty("credit_card")]
		public PayTraceCreditCard CreditCard {get; set; }		

		[Required]
		[JsonProperty("billing_address")]
		public PayTraceBillingAddress BillingAddress { get; set; }
	}

	public class PayTraceCreditCard
	{
		[Required]
		[JsonProperty("encrypted_number")]
		public string CcNumber { get; set; }

		[Required]
		[JsonProperty("encrypted_csc")]
		public string CcCsc { get; set; }

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
		public string country { get; set; }
	}
}
