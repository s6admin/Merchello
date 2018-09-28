namespace Merchello.Providers.Exceptions
{
	using System.Collections.Generic;
	using System.Linq;

	using Merchello.Core.Exceptions;
	using Payment.PayTrace.Models;

	//using PayTrace.PayTraceAPIInterfaceService.Model;

	/// <summary>
	/// An exception for PayTrace errors.
	/// </summary>
	public class PayTraceApiException : MerchelloApiException
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PayTraceApiException"/> class.
		/// </summary>
		/// <param name="message">
		/// The message.
		/// </param>
		public PayTraceApiException(string message)
			: base(message)
		{
			ErrorTypes = Enumerable.Empty<PayTraceErrorType>();
		}

		/// <summary>
		/// Gets or sets the error types.
		/// </summary>
		public IEnumerable<PayTraceErrorType> ErrorTypes { get; set; }
	}
}