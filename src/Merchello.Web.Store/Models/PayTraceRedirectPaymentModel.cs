using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Merchello.Web.Store.Models
{

	using Newtonsoft.Json;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;

	/// <summary>
	/// Model used for PayTrace Redirect payments
	/// </summary>
	public class PayTraceRedirectPaymentModel : StorePaymentModel
	{
		public string PayTraceToken { get; set; }

		/// <summary>
		/// Gets or sets the success redirect url.
		/// </summary>
		[Required]
		public string SuccessRedirectUrl { get; set; }

		[Required]
		public string CancelRedirectUrl { get; set; }

		[Required]
		public string DeclineRedirectUrl { get; set; }

		[Required]
		[JsonProperty("amount")]
		public double Amount { get; set; }

		[Required]
		[JsonProperty("invoice_id")]
		public string OrderNumber { get; set; }

		[Required]
		[JsonProperty("billing_address")]
		public PayTraceBillingAddress BillingAddress { get; set; }

		// TaxAmount is an OPTIONAL property to PayTrace, but we always want to ensure it is set in our orders
		[Required]
		[JsonProperty("tax_amount")]
		public decimal TaxAmount { get; set; }

		[JsonProperty("email")]
		public string CustomerEmail { get; set; }
		
	}		
}
