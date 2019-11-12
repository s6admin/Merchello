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
	using MC = Merchello.Providers;
	using Core.Models;
	using Umbraco.Core.Logging;

	public class PayTraceHelper
	{

		private static string PAYTRACE_ORDER_ID_DELIMITER = "_";

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
					// ORDERID, TRANSACTIONID, APPMSG, AVSRESPONSE, CSCRESPONSE, EMAIL, BNAME, CARDTYPE, EXPMNTH, EXPYR, LAST4, AMOUNT
					switch (arrPair[0].ToUpper())
					{
						case MC.Constants.PayTraceRedirect.ResponseKeys.OrderId:
							r.OrderId = arrPair[1]; 
							r.InvoiceKey = GetInvoiceKeyFromPayTraceOrderId(arrPair[1]);
							break;
						//case "RESPONSE": // This isn't in the list of returns
						//	r.ResponseMessage = arrPair[1];
						//	break;
						case MC.Constants.PayTraceRedirect.ResponseKeys.TransactionId:
                            r.TransactionId = arrPair[1];
							break;
						case MC.Constants.PayTraceRedirect.ResponseKeys.ApprovalCode:
                            r.AppCode = arrPair[1];
							// The primary inticator of a successful payment in the silent response scope is the presence of an AppCode value
							if (!r.AppCode.IsNullOrWhiteSpace())
							{
								r.Success = true;
							}
							break;
						case MC.Constants.PayTraceRedirect.ResponseKeys.ApprovalMessage:
							r.AppMsg = arrPair[1];
							break;
						case MC.Constants.PayTraceRedirect.ResponseKeys.AvsResponse:
                            r.AvsResponse = arrPair[1];	
							break;
						case MC.Constants.PayTraceRedirect.ResponseKeys.CscResponse:
                            r.CscResponse = arrPair[1];							
							break;
						case MC.Constants.PayTraceRedirect.ResponseKeys.Email:
                            r.Email = arrPair[1];
							break;
						case MC.Constants.PayTraceRedirect.ResponseKeys.BillingName:
                            r.BillingName = arrPair[1];
							break;
						case MC.Constants.PayTraceRedirect.ResponseKeys.CardType:
                            r.CardType = arrPair[1];
							break;
						case MC.Constants.PayTraceRedirect.ResponseKeys.CardExpireMonth:
                            r.CardExpireMonth = arrPair[1];
							break;
						case MC.Constants.PayTraceRedirect.ResponseKeys.CardExpireYear:
                            r.CardExpireYear = arrPair[1];
							break;
						case MC.Constants.PayTraceRedirect.ResponseKeys.CardLastFour:
                            r.CardLastFour = arrPair[1];
							break;
						case MC.Constants.PayTraceRedirect.ResponseKeys.Amount:
							r.Amount = Convert.ToDecimal(arrPair[1]);
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

		// Success and Declined return urls
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
						case MC.Constants.PayTraceRedirect.ResponseKeys.OrderId:
							r.OrderId = arrPair[1]; 
							r.InvoiceKey = GetInvoiceKeyFromPayTraceOrderId(arrPair[1]);
							break;
						case MC.Constants.PayTraceRedirect.ResponseKeys.AuthKey:
                            r.Token = arrPair[1];
							break;
						case MC.Constants.PayTraceRedirect.ResponseKeys.Email:
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

		public static string SetPayTraceOrderId(IInvoice invoice)
		{
			// Version 1: Original PayTrace OrderId was solely the Invoice PONumber
			//return invoice.PoNumber;

			// Version 2: Apply special OrderId for PayTrace so responses return an Invoice Key (strong reference) and not PONumber (soft reference)

			string ptid = string.Empty;

			if (invoice == null)
			{
				return ptid;
			}

			try
			{
				// Assume PONumber format is already v2 format (two digit year, four millisecond digits)			
				ptid = invoice.PoNumber + PAYTRACE_ORDER_ID_DELIMITER + invoice.Key.ToString().Replace("-", "");
			}
			catch (Exception ex)
			{
				LogHelper.Error(typeof(PayTraceHelper), "Error generating PayTrace Order Id for invoice " + invoice.Key, ex);
				return string.Empty;
			}

			return ptid;
		}

		public static Guid GetInvoiceKeyFromPayTraceOrderId(string payTraceOrderId)
		{

			string guidStr = payTraceOrderId.Substring(payTraceOrderId.IndexOf(PAYTRACE_ORDER_ID_DELIMITER) + PAYTRACE_ORDER_ID_DELIMITER.Length);

			Guid g = Guid.Empty;

			if (Guid.TryParse(guidStr, out g))
			{
				return g; // Success
			}
			else
			{
				Exception ex = new Exception("No Guid found in payTraceOrderId value.");
				LogHelper.Error(typeof(PayTraceHelper), "Could not parse Guid from PayTraceOrderId " + payTraceOrderId, ex);
			}

			return g; // Will return Guid.Empty
		}
	}
}
