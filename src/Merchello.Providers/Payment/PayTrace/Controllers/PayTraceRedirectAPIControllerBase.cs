
namespace Merchello.Providers.Payment.PayTrace.Controllers
{
	using System;
	using System.Collections.Generic;
	using System.Net.Http;
	using System.Threading.Tasks;
	using System.Web.Mvc;

	using Merchello.Core;
	using Merchello.Core.Gateways;
	using Merchello.Core.Logging;
	using Merchello.Core.Services;
	using Merchello.Web.Pluggable;

	using Umbraco.Core;
	using Umbraco.Web.WebApi;
	
	public abstract class PayTraceRedirectAPIControllerBase : UmbracoApiController
	{
		private readonly IMerchelloContext _merchelloContext;

		private ICustomerContext _customerContext;

		protected PayTraceRedirectAPIControllerBase()
			: this(MerchelloContext.Current)
		{

		}

		protected PayTraceRedirectAPIControllerBase(IMerchelloContext merchelloContext)
		{
			Mandate.ParameterNotNull(merchelloContext, "merchelloContext");
			_merchelloContext = merchelloContext;
		}
		
		/// <summary>
		/// Gets the <see cref="IGatewayContext"/>.
		/// </summary>
		protected IGatewayContext GatewayContext
		{
			get
			{
				return _merchelloContext.Gateways;
			}
		}

		/// <summary>
		/// Gets the <see cref="IInvoiceService"/>.
		/// </summary>
		protected IInvoiceService InvoiceService
		{
			get
			{
				return _merchelloContext.Services.InvoiceService;
			}
		}

		/// <summary>
		/// Gets the <see cref="IPaymentService"/>.
		/// </summary>
		protected IPaymentService PaymentService
		{
			get
			{
				return _merchelloContext.Services.PaymentService;
			}
		}

		/// <summary>
		/// Gets the <see cref="ICustomerContext"/>.
		/// </summary>
		protected ICustomerContext CustomerContext
		{
			get
			{
				if (_customerContext == null)
				{
					_customerContext = PluggableObjectHelper.GetInstance<CustomerContextBase>("CustomerContext", UmbracoContext);
				}
				return _customerContext;
			}
		}

		/// <summary>
		/// Handles a successful return from PayPal
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
		/// The <see cref="HttpResponseMessage"/>.
		/// </returns>
		public abstract HttpResponseMessage Success(Guid invoiceKey, Guid paymentKey, string token, string payerId);

		/// <summary>
		/// Handles a cancellation response from PayPal
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
		/// The <see cref="HttpResponseMessage"/>.
		/// </returns>
		public abstract HttpResponseMessage Cancel(Guid invoiceKey, Guid paymentKey, string token, string payerId = null);

		/// <summary>
		/// Gets the default extended log data.
		/// </summary>
		/// <returns>
		/// The <see cref="IExtendedLoggerData"/>.
		/// </returns>
		protected IExtendedLoggerData GetExtendedLoggerData()
		{
			var logData = MultiLogger.GetBaseLoggingData();
			logData.AddCategory("Controllers");
			logData.AddCategory("PayPal");

			return logData;
		}
	}
}
