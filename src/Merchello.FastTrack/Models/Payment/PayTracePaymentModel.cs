
namespace Merchello.FastTrack.Models.Payment
{
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;

	public class PayTracePaymentModel : FastTrackPaymentModel
	{
		/// <summary>
		/// Gets or sets the PayTrace Order Number.
		/// </summary>
		[Required]
		[DisplayName(@"PayTrace Order Number")]
		public string PurchaseOrderNumber { get; set; }
	}
}
