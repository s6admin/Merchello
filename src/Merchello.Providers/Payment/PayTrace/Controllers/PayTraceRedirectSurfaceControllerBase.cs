﻿using System;
using System.Web.Mvc;

using Merchello.Core.Logging;
using Merchello.Core.Models;
using Merchello.Core.Services;
using Merchello.Web.Mvc;

using Umbraco.Core;

namespace Merchello.Providers.Payment.PayTrace.Controllers
{
	public abstract class PayTraceRedirectSurfaceControllerBase : MerchelloSurfaceController, IPaymentMethodUiController
	{
		/// <summary>
		/// Gets the <see cref="IInvoiceService"/>.
		/// </summary>
		protected IInvoiceService InvoiceService
		{
			get
			{
				return MerchelloServices.InvoiceService;
			}
		}

		/// <summary>
		/// Gets the <see cref="IPaymentService"/>.
		/// </summary>
		protected IPaymentService PaymentService
		{
			get
			{
				return MerchelloServices.PaymentService;
			}
		}


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
		//public abstract ActionResult Success(Guid invoiceKey, Guid paymentKey, string token, string payerId);
		public abstract ActionResult Success(string paramList); // PayTrace Redirect returns all values as a urlencoded query string parameter named "parmList"

		/// <summary>
		/// Handles a Declined response from the PayTrace Redirect provider
		/// </summary>
		/// <param name="paramList">The parameter list.</param>
		/// <returns></returns>
		public abstract ActionResult Declined(string paramList);

		/// <summary>
		/// Gets the <see cref="IInvoice"/>.
		/// </summary>
		/// <param name="invoiceKey">
		/// The invoice key.
		/// </param>
		/// <returns>
		/// The <see cref="IInvoice"/>.
		/// </returns>
		/// <exception cref="NullReferenceException">
		/// Throws a null reference exception if the invoice was not found
		/// </exception>
		protected virtual IInvoice GetInvoice(Guid invoiceKey)
		{
			Mandate.ParameterCondition(!Guid.Empty.Equals(invoiceKey), "invoiceKey");
			var invoice = InvoiceService.GetByKey(invoiceKey);
			if (invoice == null)
			{
				throw new NullReferenceException("Invoice was not found.");
			}

			return invoice;
		}

		/// <summary>
		/// Gets the <see cref="IPayment"/>.
		/// </summary>
		/// <param name="paymentKey">
		/// The payment key.
		/// </param>
		/// <returns>
		/// The <see cref="IPayment"/>.
		/// </returns>
		/// <exception cref="NullReferenceException">
		/// Throws a null reference exception if the payment was not found
		/// </exception>
		protected virtual IPayment GetPayment(Guid paymentKey)
		{
			Mandate.ParameterCondition(!Guid.Empty.Equals(paymentKey), "paymentKey");
			var payment = PaymentService.GetByKey(paymentKey);
			if (payment == null)
			{
				throw new NullReferenceException("Payment was not found.");
			}

			return payment;
		}

		/// <summary>
		/// Gets the default extended log data.
		/// </summary>
		/// <returns>
		/// The <see cref="IExtendedLoggerData"/>.
		/// </returns>
		protected override IExtendedLoggerData GetExtendedLoggerData()
		{
			var logData = MultiLogger.GetBaseLoggingData();
			logData.AddCategory("Controllers");
			logData.AddCategory("PayTraceRedirect");

			return logData;
		}
	}
}
