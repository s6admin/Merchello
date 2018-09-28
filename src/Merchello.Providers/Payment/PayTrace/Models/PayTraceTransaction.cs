namespace Merchello.Providers.Payment.PayTrace.Models
{
	/// <summary>
	/// A model for saving PayTrace Checkout Transaction data.
	/// </summary>
	public class PayTraceTransaction
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PayTraceTransaction"/> class.
		/// </summary>
		public PayTraceTransaction()
		{
			Token = string.Empty;
			PayerId = string.Empty;
			AuthorizationTransactionId = string.Empty;
			AuthorizedAmount = string.Empty;
			CaptureTransactionId = string.Empty;
			CurrencyId = string.Empty;
			Authorized = false;
		}

		/// <summary>
		/// Gets or sets the token.
		/// </summary>
		public string Token { get; set; }

		/// <summary>
		/// Gets or sets the currency code.
		/// </summary>
		public string CurrencyCode { get; set; }

		/// <summary>
		/// Gets or sets the payer id.
		/// </summary>
		public string PayerId { get; set; }

		/// <summary>
		/// Gets or sets the checkout payment transaction id.
		/// </summary>
		public string CheckoutPaymentTransactionId { get; set; }

		/// <summary>
		/// Gets or sets the authorization transaction id.
		/// </summary>
		public string AuthorizationTransactionId { get; set; }

		/// <summary>
		/// Gets or sets the authorized amount.
		/// </summary>
		public string AuthorizedAmount { get; set; }

		/// <summary>
		/// Gets or sets the capture transaction id.
		/// </summary>
		public string CaptureTransactionId { get; set; }

		/// <summary>
		/// Gets or sets the currency id.
		/// </summary>
		public string CurrencyId { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the transaction was authorized on the PayTrace end.
		/// </summary>
		public bool Authorized { get; set; }
	}
}