using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Property case is intentional here to match the values provided from the PayTrace Redirect so we only need to use one set of constants
namespace Merchello.Providers.Payment.PayTrace.Models
{
	public class PayTraceRedirectTransaction
	{
		public PayTraceRedirectTransaction()
		{
			AUTHKEY = string.Empty;
			ORDERID = string.Empty;
			APPMSG = string.Empty;
			APPCODE = string.Empty;
			AVSRESPONSE = string.Empty;
			CSCRESPONSE = string.Empty;
			EMAIL = string.Empty;
			TRANSACTIONID = string.Empty;			
			Authorized = false;
		}

		/// <summary>
		/// Gets or sets the token. This contains the value PayTrace labels as AUTHKEY
		/// </summary>
		public string AUTHKEY { get; set; }

		public string RESPONSEMESSAGE { get; set; }

		public string ORDERID { get; set; }

		public string APPCODE { get; set; }

		public string APPMSG { get; set; }

		public string AVSRESPONSE { get; set; }

		public string CSCRESPONSE { get; set; }

		public string EMAIL { get; set; }

		/// <summary>
		/// Gets or sets the checkout payment transaction id.
		/// </summary>
		public string TRANSACTIONID { get; set; }

		public string BNAME { get; set; }

		public string CARDTYPE { get; set; }

		public string EXPMNTH { get; set; }

		public string EXPYR { get; set; }

		public string LAST4 { get; set; }

		public decimal AMOUNT { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the transaction was authorized on the provider's end
		/// </summary>
		public bool Authorized { get; set; }

		// PayTrace also can provide CANAME, EXPRMONTH, EXPRYEAR
	}
}
