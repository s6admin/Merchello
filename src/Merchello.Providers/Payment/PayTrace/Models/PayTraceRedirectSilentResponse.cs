using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Merchello.Providers.Payment.PayTrace.Models
{
	public class PayTraceRedirectSilentResponse
	{
		public bool Success { get; set; }

		public string ResponseMessage { get; set; }

		public string OrderId { get; set; }

		public Guid InvoiceKey { get; set; }

		public string TransactionId { get; set; }

		public string AppCode { get; set; }

		public string AppMsg { get; set; }

		public string AvsResponse { get; set; }

		public string CscResponse { get; set; }

		public string Email { get; set; }

		public string BillingName { get; set; }

		public string CardType { get; set; }

		public string CardExpireMonth { get; set; }

		public string CardExpireYear { get; set; }

		public string CardLastFour { get; set; }

		public decimal Amount { get; set; }
	}

	/// <summary>
	/// Extension methods for <see cref="ExpressCheckoutResponse"/>.
	/// </summary>
	[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:StaticElementsMustAppearBeforeInstanceElements", Justification = "Reviewed. Suppression is OK here.")]
	[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Reviewed. Suppression is OK here.")]
	public static class PayTraceRedirectSilentResponseExtensions
	{
		/// <summary>
		/// Shortcut check of success.
		/// </summary>
		/// <param name="response">
		/// The response.
		/// </param>
		/// <returns>
		/// The <see cref="bool"/>.
		/// </returns>
		public static bool Success(this PayTraceRedirectSilentResponse response)
		{			
			return response.Success;
		}
	}
}
