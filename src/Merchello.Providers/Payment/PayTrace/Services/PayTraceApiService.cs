namespace Merchello.Providers.Payment.PayTrace.Services
{
	using System;

	using Merchello.Providers.Payment.PayTrace.Models;

	//using global::PayTrace;

	using Umbraco.Core;

	/// <summary>
	/// Represents a PayTrace API Service.
	/// </summary>
	public class PayTraceApiService : PayTraceApiServiceBase, IPayTraceApiService
	{
		/// <summary>
		/// The <see cref="PayTraceProviderSettings"/>.
		/// </summary>
		private readonly PayTraceProviderSettings _settings;

		/// <summary>
		/// The <see cref="IPayTraceCheckoutService"/>.
		/// </summary>
		private Lazy<IPayTraceCheckoutService> _Checkout;

		/// <summary>
		/// Initializes a new instance of the <see cref="PayTraceApiService"/> class.
		/// </summary>
		/// <param name="settings">
		/// The settings.
		/// </param>
		public PayTraceApiService(PayTraceProviderSettings settings)
			: base(settings)
		{
			Mandate.ParameterNotNull(settings, "settings");
			_settings = settings;

			this.Initialize();
		}

		/// <summary>
		/// Gets the <see cref="IPayTraceCheckoutService"/>.
		/// </summary>
		public IPayTraceCheckoutService Checkout
		{
			get
			{
				return _Checkout.Value;
			}
		}

		/// <summary>
		/// Initializes the service.
		/// </summary>
		private void Initialize()
		{
			this._Checkout = new Lazy<IPayTraceCheckoutService>(() => new PayTraceCheckoutService(_settings));
		}
	}
}