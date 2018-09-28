namespace Merchello.Providers.Payment.PayTrace.Models
{
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using System.Linq;

	using Merchello.Core.Models;

	/// <summary>
	/// A model to store serialized transaction data for PayTrace  Checkout transactions.
	/// </summary>
	public class PayTraceTransactionRecord
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PayTraceTransactionRecord"/> class.
		/// </summary>
		public PayTraceTransactionRecord()
		{
			this.Success = true;
			this.Data = new PayTraceTransaction() { Authorized = false };
			this.RefundTransactions = Enumerable.Empty<PayTraceResponse>();
		}

		/// <summary>
		/// Gets or sets a value indicating whether the current transaction was successful.
		/// </summary>
		public bool Success { get; set; }

		/// <summary>
		/// Gets or sets the general or common data used by multiple API calls.
		/// </summary>
		public PayTraceTransaction Data { get; set; }

		/// <summary>
		/// Gets or sets the SetCheckout transaction response.
		/// </summary>
		public PayTraceResponse SetCheckout { get; set; }

		/// <summary>
		/// Gets or sets the GetCheckoutDetails transaction response.
		/// </summary>
		public PayTraceResponse GetCheckoutDetails { get; set; }

		/// <summary>
		/// Gets or sets the DoCheckoutPayment transaction response.
		/// </summary>
		public PayTraceResponse DoCheckoutPayment { get; set; }

		/// <summary>
		/// Gets or sets the DoAuthorization transaction response.
		/// </summary>
		public PayTraceResponse DoAuthorization { get; set; }

		/// <summary>
		/// Gets or sets the DoCapture transaction response.
		/// </summary>
		public PayTraceResponse DoCapture { get; set; }

		/// <summary>
		/// Gets or sets the RefundTransaction response.
		/// </summary>
		public IEnumerable<PayTraceResponse> RefundTransactions { get; set; }
	}

	/// <summary>
	/// Utility extensions for the <see cref="PayTraceTransactionRecord"/>.
	/// </summary>
	[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Reviewed. Suppression is OK here.")]
	internal static class PayTraceTransactionRecordExtensions
	{
		/// <summary>
		/// Gets the <see cref="PayTraceTransactionRecord"/> stored in the <see cref="IPayment"/>.
		/// </summary>
		/// <param name="payment">
		/// The payment.
		/// </param>
		/// <returns>
		/// The <see cref="PayTraceTransactionRecord"/>.
		/// </returns>
		public static PayTraceTransactionRecord GetPayTraceTransactionRecord(this IPayment payment)
		{
			return payment.ExtendedData.GetValue<PayTraceTransactionRecord>(Constants.PayTrace.ExtendedDataKeys.PayTraceTransaction);
		}

		/// <summary>
		/// Stores a <see cref="PayTraceTransactionRecord"/> into the <see cref="IPayment"/>'s <see cref="ExtendedDataCollection"/>.
		/// </summary>
		/// <param name="payment">
		/// The payment.
		/// </param>
		/// <param name="record">
		/// The record.
		/// </param>
		public static void SavePayTraceTransactionRecord(this IPayment payment, PayTraceTransactionRecord record)
		{
			payment.ExtendedData.SetValue(Constants.PayTrace.ExtendedDataKeys.PayTraceTransaction, record);
		}
	}
}