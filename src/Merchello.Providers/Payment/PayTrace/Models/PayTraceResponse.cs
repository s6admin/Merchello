
namespace Merchello.Providers.Payment.PayTrace.Models
{

	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using System.Linq;

	public class PayTraceResponse
	{
		public PayTraceResponse()
		{
			throw new NotImplementedException();
		}
		
		/// <summary>
		/// Gets or sets the token.
		/// </summary>
		public string Token { get; set; }

		/// <summary>
		/// Gets or sets the success value.
		/// </summary>
		public PayTraceAckCodeType? Ack { get; set; } // TODO PayTrace Model or remove

		/// <summary>
		/// Gets or sets the version.
		/// </summary>
		public string Version { get; set; }

		/// <summary>
		/// Gets or sets the build number.
		/// </summary>
		public string Build { get; set; }

		/// <summary>
		/// Gets or sets the errors.
		/// </summary>
		public IEnumerable<PayTraceErrorType> ErrorTypes { get; set; }

		/// <summary>
		/// Gets or sets the redirect url.
		/// </summary>
		public string RedirectUrl { get; set; }
	}

	/// <summary>
	/// Extension methods for <see cref="ExpressCheckoutResponse"/>.
	/// </summary>
	[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:StaticElementsMustAppearBeforeInstanceElements", Justification = "Reviewed. Suppression is OK here.")]
	[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Reviewed. Suppression is OK here.")]
	public static class PayTraceResponseExtensions
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
		public static bool Success(this PayTraceResponse response)
		{
			return response.Ack != null && (response.Ack == PayTraceAckCodeType.SUCCESS || response.Ack == PayTraceAckCodeType.SUCCESSWITHWARNING);
		}
	}
}
