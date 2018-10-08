﻿using System;
using System.Web;
using System.Web.UI;
using System.Net;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Merchello.Providers
{
	
	public class PayTraceOAuthTokenGenerator
	{
		public OAuthToken GetToken()
		{
			// Those URL are available at Authentication header page.
			string BaseUrl = ApiEndPointConfiguration.BaseUrl ; 
			string OAuthUrl= ApiEndPointConfiguration.UrlOAuth;
				
			// variables for request stream and Respone reader 
			Stream dataStream = null;
			StreamReader reader = null;
			WebResponse response = null;

			//object 
			OAuthToken OAuthTokenResult = new OAuthToken();

			try
			{
				// S6 Keep SecurityProtocol declarations BEFORE any webrequest.create calls ( .NET 4.5+ requirement per https://stackoverflow.com/questions/28286086/default-securityprotocol-in-net-4-5#comment-88724659 )
				//ServicePointManager.SecurityProtocol = (SecurityProtocolType)192 | (SecurityProtocolType)768 | (SecurityProtocolType)3072;
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;				

				// Create a request using a URL that can receive a post. 
				WebRequest request = WebRequest.Create(BaseUrl + OAuthUrl);

				// Set the Method property of the request to POST.
				request.Method = "POST";

				//to set HTTP version of the current request, use the Version10 and Version11 fields of the HttpVersion class.
				((HttpWebRequest)request).ProtocolVersion = HttpVersion.Version11;

				// optional - to set the Accept property, cast the WebRequest object into HttpWebRequest class
				((HttpWebRequest)request).Accept = "*/*";

				//Set the ContentType property of the WebRequest.
				request.ContentType = "application/x-www-form-urlencoded";

                // Create Request data and convert it to a byte array.
                var requestData = ApiAccessCredentials.GetFormattedRequest();

				byte[] byteArray = Encoding.UTF8.GetBytes (requestData);

				// Set the ContentLength property of the WebRequest.
				request.ContentLength = byteArray.Length ;

				/* S6 request.GetRequestStream() throws error due to insecure channel: "The request was aborted: Could not create SSL/TLS secure channel."
					https://stackoverflow.com/questions/28286086/default-securityprotocol-in-net-4-5	
					https://stackoverflow.com/questions/46929171/tls-1-2-error-could-not-create-ssl-tls-secure-channel
				*/
				// S6 Moved above before any webrequest.create calls (.NET 4.5+ requirement per https://stackoverflow.com/questions/28286086/default-securityprotocol-in-net-4-5#comment-88724659)
				//ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
				
				// Get the request stream.
				dataStream = request.GetRequestStream(); 

				// Write the data to the request stream.
				dataStream.Write(byteArray, 0, byteArray.Length);
				// Close the Stream object.
				dataStream.Close();

				// To Get the response.
				response = request.GetResponse();

				// Assuming Respose status is OK otherwise catch{} will be excuted 
				// Get the stream containing content returned by the server.
				dataStream = response.GetResponseStream();

				// Open the stream using a StreamReader for easy access.
				reader = new StreamReader (dataStream);

				// Read the content.
				string responseFromServer = reader.ReadToEnd ();

				// Display the Response content
				OAuthTokenResult = AuthTokenData(responseFromServer);

			}
			catch(WebException e) 
			{

				// This exception will be raised if the server didn't return 200 - OK within response.

				// Retrieve more information about the error 

				OAuthTokenResult.ErrorFlag = true;

				if (e.Response != null)
				{
					using (var responseStream = e.Response.GetResponseStream())
					{
						if (responseStream != null)
						{
							string temp = (new StreamReader(responseStream)).ReadToEnd();
							OAuthTokenResult.ObjError = JsonConvert.DeserializeObject<OAuthError>(temp);
						}
					}

					//Retrive http Error 
					HttpWebResponse err = (HttpWebResponse)e.Response;
					OAuthTokenResult.ObjError.HttpTokenError = ((int)err.StatusCode) + " " + err.StatusDescription;
				}	
				//Do your own error logging in this case
			}
			finally 
			{
				// Clean up the streams.
				if (null != reader) 
					reader.Close ();

				if (null != dataStream) 
					dataStream.Close ();

				if (null != response) 
					response.Close ();
			}

			//Do your code here
			return OAuthTokenResult;
		}

		protected OAuthToken AuthTokenData(string responseData)
		{
			// Create an object to parse JSON data
			OAuthToken objOauthToken = null;
		
			if (null != responseData) 
			{
				// parase JSON data into C# obj
				objOauthToken = JsonConvert.DeserializeObject<OAuthToken>(responseData);

				//optional as by default it will be false 		
				objOauthToken.ErrorFlag = false; 	
			} 
			return objOauthToken;
		}
	}

}
