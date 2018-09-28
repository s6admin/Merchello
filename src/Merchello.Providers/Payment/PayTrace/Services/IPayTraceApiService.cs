namespace Merchello.Providers.Payment.PayTrace.Services
{
	/// <summary>
	/// Defines a PayTrace API Service.
	/// </summary>
	public interface IPayTraceApiService
	{
		///// <summary>
		///// Gets the <see cref="IPayTraceApiPaymentService"/>.
		///// </summary>
		//IPayTraceApiPaymentService ApiPayment { get; }

		/// <summary>
		/// Gets the <see cref="IPayTraceCheckoutService"/>.
		/// </summary>
		IPayTraceCheckoutService Checkout { get; }

	}
}