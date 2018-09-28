namespace Merchello.Providers.Payment.PayTrace.Controllers
{
	using System;
	using System.Web.Mvc;

	using Umbraco.Web.Mvc;

	/// <summary>
	/// Defines base a PayTrace <see cref="SurfaceController"/>.
	/// </summary>
	public interface IPayTraceSurfaceController
	{
		/// <summary>
		/// Handles a successful return from PayTrace
		/// </summary>
		/// <param name="invoiceKey">
		/// The invoice key.
		/// </param>
		/// <param name="paymentKey">
		/// The payment key.
		/// </param>
		/// <param name="token">
		/// The token.
		/// </param>
		/// <param name="payerId">
		/// The payer id.
		/// </param>
		/// <returns>
		/// The <see cref="ActionResult"/>.
		/// </returns>
		ActionResult Success(Guid invoiceKey, Guid paymentKey, string token, string payerId);

		/// <summary>
		/// Handles a cancellation response from PayTrace
		/// </summary>
		/// <param name="invoiceKey">
		/// The invoice key.
		/// </param>
		/// <param name="paymentKey">
		/// The payment key.
		/// </param>
		/// <param name="token">
		/// The token.
		/// </param>
		/// <param name="payerId">
		/// The payer id.
		/// </param>
		/// <returns>
		/// The <see cref="ActionResult"/>.
		/// </returns>
		ActionResult Cancel(Guid invoiceKey, Guid paymentKey, string token, string payerId = null);
	}
}