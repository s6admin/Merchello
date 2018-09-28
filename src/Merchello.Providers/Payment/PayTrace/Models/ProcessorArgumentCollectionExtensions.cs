namespace Merchello.Providers.Payment.PayTrace.Models
{
	using Merchello.Core.Gateways.Payment;
	using Merchello.Core.Models;

	public static class ProcessorArgumentCollectionExtensions
	{
		public static void SetPayTraceExpressAjaxRequest(this ProcessorArgumentCollection args, bool value)
		{
			args.Add("payTraceExpressAjax", value.ToString());
		}

		public static bool GetPayTraceRequestIsAjaxRequest(this ExtendedDataCollection extendedData)
		{
			bool value;
			if (bool.TryParse(extendedData.GetValue("payTraceExpressAjax"), out value))
			{
				return value;
			}
			else
			{
				return false;
			}
		}
	}
}