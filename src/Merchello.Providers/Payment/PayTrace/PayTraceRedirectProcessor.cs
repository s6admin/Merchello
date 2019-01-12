namespace Merchello.Providers.Payment.PayTrace
{
	using System;

	using Merchello.Core.Models;
	using Merchello.Providers.Payment.PayPal.Models;
	using Merchello.Providers.Payment.PayPal.Services;

	using Umbraco.Core;
	using Services;
	using Models;

	internal class PayTraceRedirectProcessor
	{
		private readonly PayTraceRedirectService _service;

		public PayTraceRedirectProcessor(IPayTraceRedirectService service)
		{
			Mandate.ParameterNotNull(service, "service");
			this._service = (PayTraceRedirectService)service;
		}

		public PayTraceRedirectTransactionRecord VerifySuccessAuthorziation(IInvoice invoice, IPayment payment)
		{
			// We need to process several transactions in a row to get all the data we need to record the
			// transaction with enough information to do refunds / partial refunds
			var record = payment.GetPayTraceTransactionRecord();
			if (record == null || record.SetCheckout == null || record.Data.Token.IsNullOrWhiteSpace())
			{
				throw new NullReferenceException("PayTrace Redirect Checkout must be setup");
			}

			// From PayPal schema, unrelated to PayTrace
			//record = _service.GetExpressCheckoutDetails(payment, record.Data.Token, record);
			//if (!record.Success) return record;

			record = Process(payment, _service.DoCheckoutPayment(invoice, payment, record.Data.Token, record.Data.PayerId, record));
			if (!record.Success) return record;
			
			// This looks like a PayPal specific call because their original response isn't designed to identify success or failure
			//record = Process(payment, _service.Authorize(invoice, payment, record.Data.Token, record.Data.PayerId, record));

			return record;
		}

		private PayTraceRedirectTransactionRecord Process(IPayment payment, PayTraceRedirectTransactionRecord record)
		{
			payment.SavePayTraceTransactionRecord(record);
			return record;
		}
	}
}
