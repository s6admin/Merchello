namespace Merchello.Providers.Payment.PayTrace
{
	using Merchello.Core;
	using Merchello.Core.Gateways;
	using Merchello.Providers.Models;
	using Merchello.Providers.Payment.PayTrace.Provider;
	using Merchello.Providers.Payment.PayTrace.Models;
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using System.Web;
	using Merchello.Core.Logging;

	public class PayTraceHelper
	{
		// ISSUE: GetProviderByKey definition can't be seen in this scope
		public static PayTraceRedirectProviderSettings GetProviderSettings()
		{
			var provider = (PayTraceRedirectPaymentGatewayProvider)MerchelloContext.Current.Gateways.Payment.GetProviderByKey(Merchello.Providers.Constants.PayTraceRedirect.GatewayProviderSettingsKey);

			if (provider == null) { return null; }

			var settings = provider.ExtendedData.GetPayTraceRedirectProviderSettings();

			return settings;
		}

		public static string GetBaseWebsiteUrl()
		{
			var websiteUrl = string.Empty;
			try
			{
				var url = HttpContext.Current.Request.Url;
				websiteUrl =
					string.Format(
						"{0}://{1}{2}",
						url.Scheme,
						url.Host,
						url.IsDefaultPort ? string.Empty : ":" + url.Port).EnsureNotEndsWith('/');
			}
			catch (Exception ex)
			{
				var logData = MultiLogger.GetBaseLoggingData();
				logData.AddCategory("PayTraceRedirect");

				MultiLogHelper.WarnWithException(
					typeof(PayTraceHelper),
					"Failed to initialize factory setting for WebsiteUrl. HttpContext.Current.Request is likely null.",
					ex,
					logData);
			}

			return websiteUrl;
		}

		public static string FormatQueryString(Guid invoiceKey, Guid paymentKey)
		{
			var settings = GetProviderSettings();

			if (settings == null) return string.Empty;

			return string.Format(settings.QueryString, invoiceKey, paymentKey);
		}

		/// <summary>
		/// Parses PayTrace parameter string returned in the silent response
		/// </summary>
		/// <param name="strResponse">The string response.</param>
		/// <returns></returns>
		public static PayTraceRedirectSilentResponse ParsePayTraceSilentParamList(string strResponse)
		{
			PayTraceRedirectSilentResponse r = new PayTraceRedirectSilentResponse();
			string[] arrResponse;
			string[] arrPair;
		
			if (strResponse == null) return r;

			if(strResponse != string.Empty && strResponse.Contains("|") && strResponse.Contains("~"))
			{
				arrResponse = strResponse.Split('|'); // split the response into an array of name/ value pairs

				if (arrResponse == null) return r;

				foreach(string s in arrResponse)
				{
					arrPair = s.Split('~');

					switch (arrPair[0].ToUpper())
					{
						case "ORDERID":
							r.OrderId = arrPair[1];
							break;
						case "RESPONSE":
							r.ResponseMessage = arrPair[1];
							break;
						case "TRANSACTIONID":
							r.TransactionId = arrPair[1];
							break;
						case "APPCODE":
							r.AppCode = arrPair[1];
							break;
						case "APPMSG":
							r.AppMsg = arrPair[1];
							break;
						case "AVSRESPONSE":
							r.AvsResponse = arrPair[1];
							break;
						case "CSCRESPONSE":
							r.CscResponse = arrPair[1];
							break;
					}
					
                }
            } else
			{
				Exception ex = new Exception("Could not parse PayTrace silent response parameter list. Value is missing or invalid.");
				MultiLogHelper.Error<PayTraceHelper>(ex.Message, ex);
			}

			return r;
		}

		public static PayTraceRedirectResponse ParsePayTraceParamList(string strResponse)
		{
			PayTraceRedirectResponse r = new PayTraceRedirectResponse();
			string[] arrResponse;
			string[] arrPair;

			if (strResponse == null) return r;

			if (strResponse != string.Empty && strResponse.Contains("|") && strResponse.Contains("~"))
			{
				arrResponse = strResponse.Split('|'); // split the response into an array of name/ value pairs

				if (arrResponse == null) return r;

				foreach (string s in arrResponse)
				{
					arrPair = s.Split('~');

					switch (arrPair[0].ToUpper())
					{
						case "ORDERID":
							r.OrderId = arrPair[1];
							break;
						case "AUTHKEY":
							r.Token = arrPair[1];
							break;						
						case "EMAIL":
							r.Email = arrPair[1];
							break;
					}

				}
			}
			else
			{
				Exception ex = new Exception("Could not parse PayTrace parameter list. Value is missing or invalid.");
				MultiLogHelper.Error<PayTraceHelper>(ex.Message, ex);
			}

			return r;
		}
	}
}
