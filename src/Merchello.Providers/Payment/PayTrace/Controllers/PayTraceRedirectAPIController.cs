
namespace Merchello.Providers.Payment.PayTrace.Controllers
{

	using System;
	using System.Net;
	using System.Net.Http;
	using System.Web.Mvc;

	using Merchello.Core.Logging;
	using Merchello.Core.Models;
	using Merchello.Providers.Payment.PayTrace.Provider;

	using Umbraco.Core;
	using Umbraco.Web.Mvc;

	using Constants = Merchello.Providers.Constants;
		
	[PluginController("MerchelloProviders")]
	public class PayTraceRedirectAPIController : PayTraceRedirectAPIControllerBase
	{
		private PayTraceRedirectPaymentGatewayMethod _paymentMethod;

		public PayTraceRedirectAPIController()
		{
			this.Initialize();
		}

		[System.Web.Http.HttpGet]
		public override HttpResponseMessage Success(Guid invoiceKey, Guid paymentKey, string token, string payerId)
		{
			try
			{
				Mandate.ParameterCondition(!Guid.Empty.Equals(invoiceKey), "invoiceKey");
				Mandate.ParameterCondition(!Guid.Empty.Equals(paymentKey), "paymentKey");

				var invoice = InvoiceService.GetByKey(invoiceKey);
				if (invoice == null) throw new NullReferenceException("Invoice was not found.");

				var payment = PaymentService.GetByKey(paymentKey);
				if (payment == null) throw new NullReferenceException("Payment was not found.");

				invoice.CapturePayment(payment, _paymentMethod, invoice.Total);

				// Redirect to site
				CustomerContext.SetValue("invoiceKey", invoiceKey.ToString());
				// var returnUrl = payment.ExtendedData.GetValue(Constants.ExtendedDataKeys.ReturnUrl);
				var response = Request.CreateResponse(HttpStatusCode.Moved);
				// response.Headers.Location = new Uri(returnUrl.Replace("%INVOICE%", invoice.Key.ToString().EncryptWithMachineKey()));
				return response;

			} catch(Exception ex)
			{
				var logData = GetExtendedLoggerData();

				var extra = new
				{
					InvoiceKey = invoiceKey,
					PaymentKey = paymentKey,
					Token = token,
					PayerId = payerId
				};

				logData.SetValue<object>("extra", extra);

				MultiLogHelper.Error<PayTraceRedirectAPIController>(
					"Failed to Capture PayTrace Redirect checkout response.",
					ex,
					logData);

				return Request.CreateResponse(HttpStatusCode.InternalServerError, extra);
			}
		}

		[System.Web.Http.HttpGet]
		public override HttpResponseMessage Cancel(Guid invoiceKey, Guid paymentKey, string token, string payerId = null)
		{
			MultiLogHelper.Info<PayTraceRedirectAPIController>("Received a CANCEL.");
			throw new NotImplementedException();
		}
				
		private void Initialize()
		{
			var provider = GatewayContext.Payment.GetProviderByKey(Constants.PayTrace.GatewayProviderSettingsKey);
			if (provider == null)
			{
				var nullRef =
					new NullReferenceException(
						"PayTracePaymentGatewayProvider is not activated or has not been resolved.");
				MultiLogHelper.Error<PayTraceRedirectAPIController>(
					"Failed to find active PayTracePaymentGatewayProvider.",
					nullRef,
					GetExtendedLoggerData());

				throw nullRef;
			}

			_paymentMethod = provider.GetPaymentGatewayMethodByPaymentCode(Constants.PayTrace.PaymentCodes.RedirectCheckout) as PayTraceRedirectPaymentGatewayMethod;

			if (_paymentMethod == null)
			{
				var nullRef = new NullReferenceException("PayTraceRedirectPaymentGatewayMethod could not be instantiated");
				MultiLogHelper.Error<PayTraceRedirectAPIController>("PayTraceRedirectPaymentGatewayMethod was null", nullRef, GetExtendedLoggerData());

				throw nullRef;
			}
		}
	}
}
