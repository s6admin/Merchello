namespace Merchello.FastTrack.Controllers.Payment
{
	using System.Web.Mvc;
	using Core.Gateways;
	using Models.Payment;
	using Umbraco.Core;
	using Web.Controllers;
	using System;
	using Newtonsoft.Json;
	using Core.Gateways.Payment;
	using MC = Merchello.Providers.Constants;
	using Umbraco.Web.Mvc;
	using Web.Models.Ui;
	using Providers;
	using Web.Models.Ui.Async;
	using Web.Store.Models;
	using Core.Models;
	using Web.Store.Factories;
	using Web.Factories;
	using Umbraco.Core.Logging;
	using Core.Logging;
	using Merchello.Core.Gateways;
	using Merchello.Web.Controllers;
	using Merchello.Web.Store.Models;
	using Merchello.Web.Store.Factories;
	using Merchello.Core.Models;
	using System.Text;
	using System.Net;
	using System.IO;
	using Providers.Payment.PayTrace.Models;
	using System.Web;
	using Providers.Models;
	using Providers.Payment.PayTrace;

	// S6 This is used for the PayTrace Redirect payment methods, not the Client-side Encryption JSON payment methods
	// NOTE: The PayPal Express Checkout controller is located in Merchello.Web.Store.Controllers.Payment, not here in Merchello.FastTrack.Controllers.Payment like the other providers?
	[PluginController("FastTrack")]
	[GatewayMethodUi("PayTrace.RedirectCheckout")]
	public class PayTraceRedirectPaymentController : CheckoutPaymentControllerBase<PayTraceRedirectPaymentModel>
	{

		private const string ValidatePayUrl = "https://paytrace.com/api/validate.pay";

		private const string DefaultSuccessUrl = "";

		private const string DefaultCancelUrl = "";

		private const string DefaultDeclineUrl = "";
		       
		public PayTraceRedirectPaymentController()
			: base(new PayTraceRedirectPaymentModelFactory<PayTraceRedirectPaymentModel>())
		{ }

		/// <summary>
		/// Responsible for rendering the payment form. There are no customer-submitted values in the Redirect form, it is just a notification that they will be taken off-site to complete their transaction after clicking the button
		/// </summary>
		/// <param name="view">The optional view.</param>
		/// <returns>
		/// The <see cref="ActionResult" />.
		/// </returns>
		[ChildActionOnly]
		[GatewayMethodUi("PayTrace.RedirectCheckout")]
		public override ActionResult PaymentForm(string view = "")
		{
			var paymentMethod = this.CheckoutManager.Payment.GetPaymentMethod();
			if (paymentMethod == null) return this.InvalidCheckoutStagePartial();

			var model = this.CheckoutPaymentModelFactory.Create(CurrentCustomer, paymentMethod);

			// Set any eComm or CMS properties we don't want to scope into the PayTrace Factory

			return view.IsNullOrWhiteSpace() ? this.PartialView(model) : this.PartialView(view, model);
		}
		
		/// <summary>
		/// Begins the PayTrace Redirect process after the customer clicks the form button (no fields are submitted). This method is called PROCESS in the referenced PayPal Express provider.
		/// </summary>
		/// <returns></returns>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult HandlePaymentForm(PayTraceRedirectPaymentModel model)
		{
			
            var paymentMethod = this.CheckoutManager.Payment.GetPaymentMethod();
			if (paymentMethod == null)
			{
				var ex = new NullReferenceException("PaymentMethod was null");
				return HandlePaymentException(model, ex);
			}
						
			/*
				Redirect Providers MUST keep their basket contents until after payment is complete otherwise the checkout
				workflow will break if the customer navigates backwards or their payment fails
			*/
			CheckoutManager.Context.Settings.EmptyBasketOnPaymentSuccess = false;

			// Rebuild model to capture Billing Address/Email details which are otherwise lost after the call to AuthorizePayment below
			model = this.CheckoutPaymentModelFactory.Create(CurrentCustomer, paymentMethod); // This might be unnecessary now that OnFinalizing() call has been omitted

			/* 
				S6
				The default redirect provider implementation has a logic problem where the AuthorizePayment methods call OnFinalizing which
				in turn calls a series of Reset() methods in the CheckoutManager. This means that as the invoice is initially created before
				redirecting to the payment provider some required customer data is deleted even though the checkout isn't finished. If the 
				customer redirect payment fails or they navigate back via their browser, the checkout errors because of the missing data.
				Similar issue/resolution is here:
				https://our.umbraco.com/packages/collaboration/merchello/merchello/81312-retain-shipping-address-after-redirect

				We need to implement our own AuthorizePayment that avoids this Finalizing call
			*/

			string successUrl = System.Configuration.ConfigurationManager.AppSettings["payTraceSuccessUrl"];

			// Create zero dollar payment (promise of) so an Invoice Id is generated and can be provided to the PayTrace redirect page
			var attempt = CheckoutManager.Payment.AuthorizePayment(paymentMethod.Key, null, false); // S6 custom AuthorizePayment that doesn't force OnFinalizing()
			var resultModel = CheckoutPaymentModelFactory.Create(CurrentCustomer, paymentMethod, attempt);

			if (!attempt.Payment.Success)
			{
				LogHelper.Error(typeof(PayTraceRedirectPaymentController), "AuthorizePayment failed. ", attempt.Payment.Exception);								
				return CurrentUmbracoPage();			
			}
			// Pay Pal Express sets InvoiceKey on payment success but since client will accept orders even with failed payments, the invoiceKey should ALWAYS be set
			CustomerContext.SetValue("invoiceKey", attempt.Invoice.Key.ToString());

			string redirectUrl = string.Empty;
			if (attempt.RedirectUrl != null && attempt.RedirectUrl.Length > 0)
			{
				redirectUrl = attempt.RedirectUrl;
			}
			else
			{
				redirectUrl = ValidatePayUrl;
			}
			
			if (attempt.Invoice != null)
			{
				model.Amount = decimal.ToDouble(attempt.Invoice.Total);
				model.TaxAmount = attempt.Invoice.TotalTax();

				// https://our.umbraco.com/packages/collaboration/merchello/merchello/72986-non-inline-payment-provider				
				if (attempt.Invoice.PoNumber != null && attempt.Invoice.PoNumber.Length > 0)
				{
					model.OrderNumber = attempt.Invoice.PoNumber;
				}
			}
			
			var settings = PayTraceHelper.GetProviderSettings();			
			
			//format parameters for request 
			// to get an approval amount set: AMOUNT~1.00
			// to get a declined amount set: AMOUNT~1.12
			string parameters = string.Empty;

			parameters += "UN~" + ApiAccessCredentials.UserName + "|";
			parameters += "PSWD~" + ApiAccessCredentials.Password + "|";
			parameters += "ORDERID~" + model.OrderNumber + "|";
			parameters += "AMOUNT~" + model.Amount + "|";
			parameters += "TERMS~Y|TRANXTYPE~Sale|";
						
			// NOTE: These urls override any settings set in the PayTrace admin dashboard	
			// TODO Pull them from the PayTraceRedirectProviderSettings if within scope		
			parameters += "ApproveURL~" + settings.SuccessUrl + "|";
			parameters += "DeclineURL~" + settings.DeclinedUrl + "|"; // If declined, send customer back to payment page otherwise create a custom landing page with a declined message
			parameters += "ReturnURL~" + settings.SilentResponseUrl + "|"; // SilentUrl isn't permitted as a url parameter but documentation says ReturnUrl is an alternate target for the silet POST if one isn't specified in the PayTrace manager dashboard

			string url = SendValidationRequest(parameters);

			if(url.Length > 0 && url.StartsWith("https://paytrace.com/"))
			{
				#region Optional Parameters Reference
				/* Optional parameters for redirect 

					BNAME, BADDRESS, BADDRESS2, BCITY, BSTATE, BZIP, BCOUNTRY, EMAIL, PHONE, INVOICE, and DESCRIPTION may all be defaulted to information that you may have already collected from the customer.

					DISABLELOGIN may be set to ‘Y’ to prevent customers from being able to log into their account.
					DISABLEOPTIONAL may be set to ‘Y’ to hide optional data fields.
					SHOWBNAME may be set to ‘Y’ to include the billing name when DISABLEOPTIONAL is set to ‘Y’
					HIDEDESCRIPTION may be set to ‘Y’ to hide the description value on the checkout page and receipt.
					HIDEINVOICE may be set to ‘Y’ to hide the invoice value on the receipt.
					HIDEPASSWORD may be set to ‘Y’ to prevent customers from being able to log into their account. This will also prevent customers from being prompted to provide a password. Passing this parameter will use the "OrderID" as the CustomerID value in any instance where the customer profile is being created.
					RETURNPARIS may be set to ‘Y’ to have additional data values including BNAME, CARDTYPE, EXPMNTH, and EXPYR in the silent post response. A 2 digit abbreviation for the card type will be returned on the MES, NGT and Paymentech processing networks. While TSYS, FirstData and Global networks will show the full label of the card type.
					ENABLEREDIRECT may be set to ‘Y’ to force customers to be redirected to your approval/decline URL once the payment is complete.
					ENABLESWIPE may be set to ‘Y’ to allow cardholders to swipe their cards into the checkout page.
					TEST may be set to ‘Y’ to treat the transaction as a test.
					DISPLAYTRUSTLOGO may be set to ‘Y’ to display a security trust logo on the checkout page.
					DISABLETERMS may be set to ‘Y’ to hide the payment terms link and checkbox.
					CANCELURL may be set to the URL where the user should be taken if they choose to cancel/revise their payment.
					DISABLERECEIPT may be set to ‘Y’ to prevent receipts from being sent to customers or merchants.
					IMAGEURL may be set to the dynamic image that should be displayed in the header of the Secure Checkout page.
					TERMSURL may be set to the URL where the terms and conditions of the transaction are located to be viewed.
					PRODUCTDETAILS may be set to HTML code that will display information about the payment to the user. For example, the following value may be included to display a table of product information to the user:
					CUSTOMDBA may be set to the custom DBA that should be displayed in the Secure Checkout and be passed to the acquirer and reflect on the customers statement in place of the DBA on file with the PayTrace account.

				*/
				#endregion Optional Parameters Reference
				string op = string.Empty;

				
				op += "BNAME~" + WebUtility.UrlEncode(model.BillingAddress.Name) + "|";
				op += "BADDRESS~" + WebUtility.UrlEncode(model.BillingAddress.StreetAddress) + "|";
				op += "BADDRESS2~" + WebUtility.UrlEncode(model.BillingAddress.StreetAddress2) + "|";
				op += "BCITY~" + WebUtility.UrlEncode(model.BillingAddress.City) + "|";
				op += "BSTATE~" + WebUtility.UrlEncode(model.BillingAddress.State) + "|";
				op += "BZIP~" + WebUtility.UrlEncode(model.BillingAddress.Zip) + "|";
				op += "BCOUNTRY~" + WebUtility.UrlEncode(model.BillingAddress.Country) + "|";
				op += "EMAIL~" + WebUtility.UrlEncode(model.CustomerEmail) + "|"; // TODO Email is null
				//op += "PHONE~" + "|"; // Phone isn't present in either Customer or Address details, but might not be important to process the transaction anyway
				op += "RETURNPARIS~Y|";
				op += "ENABLEREDIRECT~Y" + "|";
				//op += "TEST~Y" + "|";

				// Parameter structure
				// op += "~" + "|"; 

				// Append encoded optional parameters
				url += op;

				return Redirect(url);
			} else
			{
				return CurrentUmbracoPage();
			}
		}

		protected override ActionResult HandlePaymentException(PayTraceRedirectPaymentModel model, Exception ex)
		{

			//// Keep track of the failed attempts in the Customer data so the next checkout step is aware of the payment failure(s)
			//int attempts = 1;
			//ExtendedDataCollection ed = CheckoutManager.Customer.Context.Customer.ExtendedData;

			//// Retrieve previous saved value if it exists
			//if (ed.ContainsKey(MC.PayTraceRedirect.ExtendedDataKeys.FailedAttempts))
			//{
			//	int.TryParse(ed.GetValue(MC.PayTraceRedirect.ExtendedDataKeys.FailedAttempts), out attempts); // Retrieve previous value
			//	attempts = attempts + 1; // Increment previous value				
			//}

			//ed.SetValue(MC.PayTraceRedirect.ExtendedDataKeys.FailedAttempts, attempts.ToString());
			//ViewData[MC.PayTraceRedirect.ExtendedDataKeys.FailedAttempts] = attempts;

			return base.HandlePaymentException(model, ex);
		}

		/// <summary>
		/// Sends the validation request to PayTrace.
		/// </summary>
		/// <param name="parameters">The parameters.</param>
		private string SendValidationRequest(string parameters)
		{
			string parameter_list = "PARMLIST=";

			parameter_list = parameter_list + parameters;
			ASCIIEncoding encoding = new ASCIIEncoding();
			byte[] bytes = encoding.GetBytes(parameter_list);
						
			// S6 Keep SecurityProtocol configuration BEFORE any web request create commands!
			ServicePointManager.Expect100Continue = true;			
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12; // S6 Original protocol specified as Ssl3 but that does not connect successfully

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(ValidatePayUrl);
			request.Method = "POST";
			request.ContentType = "application/x-www-form-urlencoded";
			request.ContentLength = bytes.Length;
			
			// Establish test certificate
			// https://serverfault.com/questions/131046/how-to-grant-iis-7-5-access-to-a-certificate-in-certificate-store/132791#132791

			// send validation request
			Stream str = null;

			try
			{
				str = request.GetRequestStream();
				str.Write(bytes, 0, bytes.Length);
				str.Flush();
				str.Close();
			} catch(Exception ex)
			{
				// Likely TLS and network security errors can occur here
				LogHelper.Error(typeof(PayTraceRedirectPaymentController), ex.Message, ex);
				return string.Empty;
			}
			
			// get response and parse
			WebResponse response = request.GetResponse();
			Stream rsp_stream = response.GetResponseStream();
			StreamReader reader = new StreamReader(rsp_stream);

			// read the response string
			string strResponse = reader.ReadToEnd();

			if (ParsePayTraceResponse(strResponse))
			{
				string url = "https://paytrace.com/api/checkout.pay?parmList=orderID~{0}|AuthKey~{1}|";
				try
				{
					url = string.Format(url, (string)Session["OrderID"], (string)Session["AuthKey"]);
					
					// NOTE: Optional parameters are set after a successful url base is returned
										
					return url; // Success

				} catch(Exception ex)
				{
					LogHelper.Error(typeof(PayTraceRedirectPaymentController), ex.Message, ex);
				}
			}

			return string.Empty; // Problem(s) with validation call or redirect url prep
		}
		
		private bool ParsePayTraceResponse(string strResponse)
		{
			// if we have errors if so output to ui
			if (!strResponse.Contains("ERROR"))
			{				
				try
				{
					string[] parameters = strResponse.Split('|');
					string OrderID = parameters[0].Split('~')[1];
					string AUTHKEY = parameters[1].Split('~')[1];
					Session["OrderID"] = OrderID;
					Session["AuthKey"] = AUTHKEY;
				} catch(Exception ex)
				{
					// If any required parameters are missing or invalid
					LogHelper.Error(typeof(PayTraceRedirectPaymentController), ex.Message, ex);
					return false;
				}				
			}
			else
			{
				// Response contains flagged errors
				LogHelper.Error(typeof(PayTraceRedirectPaymentController), strResponse, new Exception("PayTrace Redirect response has ERRORS. "));
				return false;
			}

			return true;
		}
		
	}
}
