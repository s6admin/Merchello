using Merchello.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// TODO This may be similar to a PayTraceRedirectResponse object. If so they might be mergeable.
namespace Merchello.Providers.Payment.PayTrace.Models
{
	/// <summary>
	/// A model to store serialized transaction data for PayTrace Redirect transactions.
	/// </summary>
	public class PayTraceRedirectTransactionRecord
	{
		public PayTraceRedirectTransactionRecord()
		{
			this.Success = true;
			this.Data = new PayTraceRedirectTransaction() { Authorized = false };
			this.RefundTransactions = Enumerable.Empty<PayTraceRedirectResponse>();
		}

		/// <summary>
		/// Gets or sets a value indicating whether the current transaction was successful.
		/// </summary>
		public bool Success { get; set; }
		
		/// <summary>
		/// Gets or sets the general or common data used by multiple API calls.
		/// </summary>
		public PayTraceRedirectTransaction Data { get; set; }

		/// <summary>
		/// Gets or sets the SetCheckout transaction response.
		/// </summary>
		public PayTraceRedirectResponse SetCheckout { get; set; }

		/// <summary>
		/// Gets or sets the GetCheckoutDetails transaction response.
		/// </summary>
		public PayTraceRedirectResponse GetCheckoutDetails { get; set; }

		/// <summary>
		/// Gets or sets the DoCheckoutPayment transaction response.
		/// </summary>
		public PayTraceRedirectResponse DoCheckoutPayment { get; set; }

		/// <summary>
		/// Gets or sets the DoAuthorization transaction response.
		/// </summary>
		public PayTraceRedirectResponse DoAuthorization { get; set; }

		/// <summary>
		/// Gets or sets the DoCapture transaction response.
		/// </summary>
		public PayTraceRedirectResponse DoCapture { get; set; }

		/// <summary>
		/// Gets or sets the RefundTransaction response.
		/// </summary>
		public IEnumerable<PayTraceRedirectResponse> RefundTransactions { get; set; }
	}

	/// <summary>
	/// Utility extensions for the <see cref="PayTraceRedirectTransactionRecord"/>.
	/// </summary>
	[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Reviewed. Suppression is OK here.")]
	internal static class PayTraceRedirectTransactionRecordExtensions
	{
		/// <summary>
		/// Gets the <see cref="PayTraceRedirectTransactionRecord"/> stored in the <see cref="IPayment"/>.
		/// </summary>
		/// <param name="payment">
		/// The payment.
		/// </param>
		/// <returns>
		/// The <see cref="PayTraceRedirectTransactionRecord"/>.
		/// </returns>
		public static PayTraceRedirectTransactionRecord GetPayTraceTransactionRecord(this IPayment payment)
		{
			return payment.ExtendedData.GetValue<PayTraceRedirectTransactionRecord>(Constants.PayTrace.ExtendedDataKeys.PayTraceRedirectTransaction);
		}

		/// <summary>
		/// Stores a <see cref="PayTraceRedirectTransactionRecord"/> into the <see cref="IPayment"/>'s <see cref="ExtendedDataCollection"/>.
		/// </summary>
		/// <param name="payment">
		/// The payment.
		/// </param>
		/// <param name="record">
		/// The record.
		/// </param>
		public static void SavePayTraceTransactionRecord(this IPayment payment, PayTraceRedirectTransactionRecord record)
		{
			payment.ExtendedData.SetValue(Constants.PayTrace.ExtendedDataKeys.PayTraceRedirectTransaction, record);
		}
	}
}
