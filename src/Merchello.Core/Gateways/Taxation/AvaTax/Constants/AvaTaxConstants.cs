using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Merchello.Core.Gateways.Taxation.AvaTax.Constants
{
	/// <summary>
	/// AvaTax Constants
	/// </summary>
	public static class AvaTaxConstants
	{
		// TODO These need to be pulled from the website project, not eComm
		public static readonly string COMPANY_CODE = ""; //ConfigurationManager.AppSettings["avaTaxCompanyCode"].ToString();
		public static readonly string COMPANY_ID = ""; //ConfigurationManager.AppSettings["avaTaxCompanyId"].ToString();
		public static readonly string ACCOUNT_ID = ""; //ConfigurationManager.AppSettings["avaTaxAccountId"].ToString();
		
		/// <summary>
		/// Dictionary Key for retrieving an AvaTax Item Code stored in an ExtendedData collection
		/// </summary>
		public static readonly string ITEM_CODE_KEY = "AVA_ITEM_CODE";

		/// <summary>
		/// Dictionary Key for retrieving an AvaTax Sales Order Transaction Model stored in an ExtendedData collection
		/// </summary>
		public static readonly string SALES_ORDER_KEY = "AVA_SALES_ORDER";

		/// <summary>
		/// Dictionary key for retrieving an AvaTax Sales Order TransactionLineModel stored in an eComm Invoice LineItem ExtendedData collection
		/// </summary>
		public static readonly string TRANSACTION_LINE = "AVA_TRANSACTION_LINE";

		/// <summary>
		/// A dictionary key indicating that the related ITaxCalculationResult still needs to be processed by the external AvaTax provider.
		/// </summary>
		public static readonly string AWAITING_AVATAX_KEY = "AWAITING_AVATAX";

		public static readonly string DEFAULT_TAX_CODE = "P0000000";

		/*
		 ShipFrom	Origin	The origination address where the products were shipped from, or from where the services originated.
		ShipTo	Destination	The destination address where the products were shipped to, or where the services were delivered.
		Point of Order Origin		The place of business where you receive the customer's order. This address type is valid in the United States only and only applies to tangible personal property.
		Point of Order Acceptance		The place of business where you accept/approve the customer's order, thereby becoming contractually obligated to make the sale. This address type is valid in the United States only and only applies to tangible personal property.
		 */

		/*
		 InventoryTransferInvoice	Permanent	A finalized shipment of inventory from one location to another
		InventoryTransferOrder	Temporary	An estimate for shipping inventory from one location to another
		PurchaseInvoice	Permanent	A purchase made from a vendor
		PurchaseOrder	Temporary	A quote for identifying estimated tax to pay to a vendor
		ReturnInvoice	Permanent	A finalized refund given to a customer
		ReturnOrder	Temporary	A quote for a refund to a customer
		SalesInvoice	Permanent	A finalized sale made to a customer
		SalesOrder	Temporary	A quote for a potential sale
		 */

		/*
		 * Root-level Reference Fields (client-defined, optional)
		  ReferenceCode	This field can link to the unique ID number of the invoice in your existing accounting system.
		PurchaseOrderNo	Intended to match to your customer's purchase order number, if one was provided.
		SalespersonCode	When tracking performance by salesperson, or identifying orders written by certain sales team members, this code can help you identify the author of the invoice.
		Description	A general purpose description of the invoice or transaction, or a comment explaining the transaction.
		PosLaneCode	If this transaction was made at a retail cash register, this code can be used to identify which cash register made the transaction.
		Email	The email address of the customer who requested the sale.
		 * */

		/*
		 * Line-level Reference Fields
		 * Description	Field provided to describe the item/service/shipping method for that given line. NOTE: If you participate in Streamlined Sales Tax, this field is required to be an accurate description of the product. Otherwise, it is optional and has no requirements.
			RevenueAccount	If your user wished to track this line item to a specific revenue account number in their accounting system, you could specify the revenue account number here.
			Ref1	A user-supplied reference code for this line.
			Ref2	A user-supplied reference code for this line.
		  */

		/* 
		 SalesInvoice Transaction Stages/Status
		 Saved
		 Posted
		 Committed
		 Locked
		 */
	}
}
