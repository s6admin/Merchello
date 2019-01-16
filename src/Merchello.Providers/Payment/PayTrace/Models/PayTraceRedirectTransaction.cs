using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Merchello.Providers.Payment.PayTrace.Models
{
	public class PayTraceRedirectTransaction
	{
		public PayTraceRedirectTransaction()
		{
			Token = string.Empty;
			OrderId = string.Empty;
			AppMsg = string.Empty;
			AvsResponse = string.Empty;
			CscResponse = string.Empty;
			Authorized = false;
		}

		/// <summary>
		/// Gets or sets the token.
		/// </summary>
		public string Token { get; set; }

		// The below properties are repeated in PayTraceReidrectSilentResponse. Can we combine them to keep things DRY?
		public string OrderId { get; set; }

		public string AppMsg { get; set; }

		public string AvsResponse { get; set; }

		public string CscResponse { get; set; }

		public string Email { get; set; }

		/// <summary>
		/// Gets or sets the checkout payment transaction id.
		/// </summary>
		public string TransactionId { get; set; }
		
		/// <summary>
		/// Gets or sets a value indicating whether the transaction was authorized on the provider's end
		/// </summary>
		public bool Authorized { get; set; }
	}
}
