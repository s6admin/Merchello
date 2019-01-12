
using Newtonsoft.Json;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Merchello.Web.Store.Models
{
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
