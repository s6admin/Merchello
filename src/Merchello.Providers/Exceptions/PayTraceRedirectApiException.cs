using Merchello.Core.Exceptions;
using Merchello.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Logging;

namespace Merchello.Providers.Exceptions
{
	public class PayTraceRedirectApiException : MerchelloApiException
	{

		public PayTraceRedirectApiException(string message)
			: base(message)
		{
			Exception ex = new Exception(message);
			LogHelper.Error<PayTraceRedirectApiException>(message, ex);
		}		
	}
}
