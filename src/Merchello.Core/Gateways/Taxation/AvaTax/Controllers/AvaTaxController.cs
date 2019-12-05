using System;
using System.Web.Mvc;

using Merchello.Core.Logging;
using Merchello.Core.Models;
using Merchello.Core.Services;

using Umbraco.Core;
using Umbraco.Web.Mvc;
using Umbraco.Core.Events;
using Avalara.AvaTax.RestClient;

// S6 NOT CURRENTLY USED...ATTEMPTING TO RAISE EVENTS DIRECTLY FROM AVATAX STRATEGY INSTEAD OF A CUSTOM CONTROLLER

namespace Merchello.Core.Gateways.Taxation.AvaTax.Controllers
{
	/// <summary>
	/// 
	/// </summary>
	[PluginController("Merchello")]
	public class AvaTaxController  // : MerchelloSurfaceController
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AvaTaxController"/> class.
		/// </summary>
		public AvaTaxController()
		{
			
		}

		public static event TypedEventHandler<AvaTaxController, CancellableObjectEventArgs<TransactionModel>> OnSalesOrderSuccess;
		public static event TypedEventHandler<AvaTaxController, CancellableObjectEventArgs<TransactionModel>> OnSalesOrderFail;
		public static event TypedEventHandler<AvaTaxController, CancellableObjectEventArgs<TransactionModel>> OnSalesInvoiceSuccess;
		public static event TypedEventHandler<AvaTaxController, CancellableObjectEventArgs<TransactionModel>> OnSalesInvoiceFail;

		/// <summary>
		/// Saleses the order success.
		/// </summary>
		/// <returns></returns>
		//public ActionResult SalesOrderSuccess()
		//{			
		//	//OnSalesOrderSuccess.RaiseEvent(new CancellableObjectEventArgs<TransactionModel>(tm), this);
		//}
	}
}
