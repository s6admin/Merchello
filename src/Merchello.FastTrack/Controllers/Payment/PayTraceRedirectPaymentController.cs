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

	// S6 This is used for the PayTrace Redirect payment methods, not the Client-side Encryption JSON payment methods
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

			// Preparing the invoice saves and generates a key. This should only be done once per checkout unless the basket is invalidated
			IInvoice invoice = PrepareInvoiceOnce();
			if (invoice != null)
			{
				model.Amount = decimal.ToDouble(invoice.Total);
				model.TaxAmount = invoice.TotalTax();
				model.OrderNumber = invoice.PoNumber;
			}

			return view.IsNullOrWhiteSpace() ? this.PartialView(model) : this.PartialView(view, model);
		}

		/// <summary>
		/// Begins the PayTrace Redirect process after the customer clicks the form button (no fields are submitted).
		/// </summary>
		/// <returns></returns>
		public ActionResult HandlePaymentForm(PayTraceRedirectPaymentModel model)
		{
			// Redirect PaymentForm doesn't have any submitable fields so just rebuild the model as is done in the render method so we can repopulate Billing Address and Order details
			var paymentMethod = this.CheckoutManager.Payment.GetPaymentMethod();
			if (paymentMethod == null) return this.InvalidCheckoutStagePartial();
			model = this.CheckoutPaymentModelFactory.Create(CurrentCustomer, paymentMethod);

			IInvoice invoice = PrepareInvoiceOnce();
			if (invoice != null)
			{
				model.Amount = decimal.ToDouble(invoice.Total);
				model.TaxAmount = invoice.TotalTax();
				model.OrderNumber = invoice.PoNumber;
			}

			//format parameters for request 
			// to get an approval amount set: AMOUNT~1.00
			// to get a declined amount set: AMOUNT~1.12
			string parameters = string.Empty;

			// Test data
			//parameters += "UN~demo123|PSWD~demo123|TERMS~Y|TRANXTYPE~Sale|";
			//parameters += "ORDERID~1234|AMOUNT~1.00|";

			parameters += "UN~" + ApiAccessCredentials.UserName + "|";
			parameters += "PSWD~" + ApiAccessCredentials.Password + "|";
			parameters += "ORDERID~" + model.OrderNumber + "|";
			parameters += "AMOUNT~" + model.Amount + "|";
			parameters += "TERMS~Y|TRANXTYPE~Sale|";
			
			string return_url = @"http://" + Request.Url.Authority;

			// TODO Change Urls and externalize
			parameters += "ApproveURL~" + return_url + "/receipt" + "|";
			parameters += "DeclineURL~" + return_url + "/checkout/payment" + "|"; // If declined, send customer back to payment page otherwise create a custom landing page with a declined message
			
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

				// TODO Validate optional parameter url-encoding
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
				op += "TEST~Y" + "|";
				
				// op += "~" + "|";

				// Append encoded optional parameters
				url += op;

				return Redirect(url);
			} else
			{
				return CurrentUmbracoPage();
			}
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
		
		// Special method for ensuring an invoice is only prepared once during the PaymentForm and/or Process checkout since we allow multiple payment attempts in case of gateway failure(s)
		private IInvoice PrepareInvoiceOnce()
		{
			IInvoice invoice = null;
			Guid invoiceKey = Guid.Empty;

			// Check for an invoice key in customer context so duplicate invoices aren't created (ie.) during payment failures
			try
			{
				invoiceKey = new Guid(CustomerContext.GetValue("invoiceKey"));
			}
			catch (Exception ex)
			{

			}
			if (invoiceKey.Equals(Guid.Empty))
			{
				// Prepare invoice for initial payment attempt
				invoice = this.CheckoutManager.Payment.PrepareInvoice();
			}
			else
			{
				/* 
					An invoice key is already present in customer context which means a previous payment attempt was made and failed
					In this case we want to retrieve the existing invoice, not create duplicates for each payment attempt					
				*/
				invoice = CheckoutManager.Context.Services.InvoiceService.GetByKey(invoiceKey);
			}

			return invoice;
		}

	}
}
