using Merchello.Providers.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace Merchello.Providers.Payment.PayTrace.Models
{
	public class PayTraceRedirectProviderSettings : IPaymentProviderSettings
	{

		/// <summary>
		/// The default return URL that a completed PayTrace Redirect transaction should call. This URL will process the payment within eComm and then redirect the user to the DefaultReceiptUrl
		/// </summary>
		private const string DefaultReturnUrl = "{0}/umbraco/merchello/paytraceredirect/success";

		private const string DefaultCancelUrl = "{0}/umbraco/merchello/paytraceredirect/cancel";

		private const string DefaultSilentResponseUrl = "{0}/umbraco/merchello/paytraceredirect/paytracesilentresponse"; // Params are sent and defined by PayTrace
				
		private const string DefaultRetryUrl = "{0}/umbraco/merchello/paytraceredirect/retry";

		private const string DefaultDeclinedUrl = "{0}/umbraco/merchello/paytraceredirect/declined";

		/// <summary>
		/// The default Url the Customer will see upon a successful PayTrace Redirect transaction (after the ReturnUrl has completed processing the tranaction in eComm).
		/// </summary>
		private const string DefaultReceiptUrl = "{0}/receipt";

		private const string DefaultQueryString = "?invoiceKey={1}&paymentKey={2}";

		public PayTraceRedirectProviderSettings()
		{			
			var siteUrl = PayTraceHelper.GetBaseWebsiteUrl();
            SuccessUrl = string.Format(DefaultReturnUrl, siteUrl);
			CancelUrl = string.Format(DefaultCancelUrl, siteUrl); 
			SilentResponseUrl = string.Format(DefaultSilentResponseUrl, siteUrl); 
			RetryUrl = string.Format(DefaultRetryUrl, siteUrl); 
			DeclinedUrl = string.Format(DefaultDeclinedUrl, siteUrl);
			EndUrl = string.Format(DefaultReceiptUrl, siteUrl);
			QueryString = DefaultQueryString;
			DeleteInvoiceOnCancel = false;
		}
		
		/// <summary>
		/// Gets or sets the client id.
		/// </summary>
		public string ClientId { get; set; }

		/// <summary>
		/// Gets or sets the client secret.
		/// </summary>
		//public string ClientSecret { get; set; }

		/// <summary>
		/// Gets or sets the API username.
		/// </summary>
		//public string ApiUsername { get; set; }

		/// <summary>
		/// Gets or sets the API password.
		/// </summary>
		//public string ApiPassword { get; set; }

		/// <summary>
		/// Gets or sets the API signature.
		/// </summary>
		//public string ApiSignature { get; set; }

		/// <summary>
		/// Gets or sets the application id.
		/// </summary>
		//public string ApplicationId { get; set; }

		/// <summary>
		/// Gets or sets the success url.
		/// </summary>
		public string SuccessUrl { get; set; }

		/// <summary>
		/// Gets or sets the retry url.
		/// </summary>
		public string RetryUrl { get; set; }

		/// <summary>
		/// Gets or sets the cancel url.
		/// </summary>
		public string CancelUrl { get; set; }

		/// <summary>
		/// Gets or sets the declined URL.
		/// </summary>
		/// <value>
		/// The declined URL.
		/// </value>
		public string DeclinedUrl { get; set; }

		/// <summary>
		/// Gets or sets the PayTrace silent response URL.
		/// </summary>
		/// <value>
		/// The silent response URL.
		/// </value>
		public string SilentResponseUrl { get; set; }

		public string QueryString { get; set; }

		public string EndUrl { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether to delete the invoice on cancel.
		/// </summary>
		public bool DeleteInvoiceOnCancel { get; set; }
		
	}
}
