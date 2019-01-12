using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Merchello.Providers.Payment.PayTrace.Models
{
	/// <summary>
	/// A Model for working with PayTrace Redirect provider response data
	/// </summary>
	public class PayTraceRedirectResponse
	{
		public string RedirectUrl { get; set; }

		public bool Success { get; set; }		
	}

	/// <summary>
	/// Extension methods for <see cref="ExpressCheckoutResponse"/>.
	/// </summary>
	[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:StaticElementsMustAppearBeforeInstanceElements", Justification = "Reviewed. Suppression is OK here.")]
	[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Reviewed. Suppression is OK here.")]
	public static class PayTraceRedirectResponseExtensions
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
		public static bool Success(this PayTraceRedirectResponse response)
		{
			// TODO There will likely be other factors to flagging payment success
			return response.Success;			
		}
	}
}
