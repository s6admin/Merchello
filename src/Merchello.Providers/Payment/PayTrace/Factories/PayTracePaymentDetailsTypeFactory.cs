namespace Merchello.Providers.Payment.PayTrace.Factories
{
    using System.Collections.Generic;
    using System.Linq;

    using Merchello.Core.Models;
    using Merchello.Web;
    using Merchello.Web.Models.VirtualContent;

	//using global::PayTrace.PayTraceAPIInterfaceService.Model;
	using PayTrace.Models;

    using Umbraco.Core;

    /// <summary>
    /// A factory responsible for building <see cref="PaymentDetailsItemType"/>.
    /// </summary>
    public class PayTracePaymentDetailsTypeFactory
    {
        /// <summary>
        /// The <see cref="MerchelloHelper"/>.
        /// </summary>
        private readonly MerchelloHelper _merchello = new MerchelloHelper();

        /// <summary>
        /// The base url.
        /// </summary>
        private readonly PayTraceFactorySettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="PayTracePaymentDetailsTypeFactory"/> class.
        /// </summary>
        public PayTracePaymentDetailsTypeFactory()
            : this(new PayTraceFactorySettings())
        { 
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PayTracePaymentDetailsTypeFactory"/> class.
        /// </summary>
        /// <param name="settings">
        /// The PayTrace factory settings.
        /// </param>
        public PayTracePaymentDetailsTypeFactory(PayTraceFactorySettings settings)
        {
            Mandate.ParameterNotNull(settings, "settings");
            _settings = settings;
        }

        /// <summary>
        /// Builds the <see cref="PaymentDetailsItemType"/>.
        /// </summary>
        /// <param name="invoice">
        /// The invoice.
        /// </param>
        /// <param name="actionCode">
        /// The <see cref="PaymentActionCodeType"/>.
        /// </param>
        /// <returns>
        /// The <see cref="PaymentDetailsType"/>.
        /// </returns>
        public PaymentDetailsType Build(IInvoice invoice, PaymentActionCodeType actionCode)
        {
            // Get the decimal configuration for the current currency
            var currencyCodeType = PayTraceApiHelper.GetPayTraceCurrencyCode(invoice.CurrencyCode);
            var basicAmountFactory = new PayTraceBasicAmountTypeFactory(currencyCodeType);

            // Get the tax total
            var itemTotal = basicAmountFactory.Build(invoice.TotalItemPrice() - invoice.TotalDiscounts());
            var shippingTotal = basicAmountFactory.Build(invoice.TotalShipping());
            var taxTotal = basicAmountFactory.Build(invoice.TotalTax());
            var invoiceTotal = basicAmountFactory.Build(invoice.Total);

            var items = BuildPaymentDetailsItemTypes(invoice.ProductLineItems(), basicAmountFactory).ToList();

            if (invoice.DiscountLineItems().Any())
            {
                var discounts = BuildPaymentDetailsItemTypes(invoice.DiscountLineItems(), basicAmountFactory, true);
                items.AddRange(discounts);
            }

            var paymentDetails = new PaymentDetailsType
            {
                PaymentDetailsItem = items.ToList(),
                ItemTotal = itemTotal,
                TaxTotal = taxTotal,
                ShippingTotal = shippingTotal,
                OrderTotal = invoiceTotal,
                PaymentAction = actionCode,
                InvoiceID = invoice.PrefixedInvoiceNumber()
            };

            // ShipToAddress
            if (invoice.ShippingLineItems().Any())
            {
                var addressTypeFactory = new PayTraceAddressTypeFactory();
                paymentDetails.ShipToAddress = addressTypeFactory.Build(invoice.GetShippingAddresses().FirstOrDefault());
            }

            return paymentDetails;
        }

        /// <summary>
        /// Builds a list of <see cref="PaymentDetailsItemType"/>.
        /// </summary>
        /// <param name="invoice">
        /// The invoice.
        /// </param>
        /// <param name="factory">
        /// The <see cref="PayTraceBasicAmountTypeFactory"/>.
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable{PaymentDetailsItemType}"/>.
        /// </returns>
        public virtual IEnumerable<PaymentDetailsItemType> BuildPaymentDetailsItemTypes(IInvoice invoice, PayTraceBasicAmountTypeFactory factory)
        {
            var paymentDetailItems = new List<PaymentDetailsItemType>();
            paymentDetailItems.AddRange(BuildPaymentDetailsItemTypes(invoice.ProductLineItems(), factory));
            paymentDetailItems.AddRange(BuildPaymentDetailsItemTypes(invoice.CustomLineItems(), factory));
            paymentDetailItems.AddRange(BuildPaymentDetailsItemTypes(invoice.DiscountLineItems(), factory, true));

            return paymentDetailItems;
        }

        /// <summary>
        /// Builds a list of <see cref="PaymentDetailsItemType"/>.
        /// </summary>
        /// <param name="items">
        /// The items.
        /// </param>        
        /// <param name="factory">
        /// The <see cref="PayTraceBasicAmountTypeFactory"/>.
        /// </param>
        /// <param name="areDiscounts">
        /// The are discounts.
        /// </param>
        /// <returns>
        /// The <see cref="IEnumerable{PaymentDetailItemType}"/>.
        /// </returns>
        public virtual IEnumerable<PaymentDetailsItemType> BuildPaymentDetailsItemTypes(IEnumerable<ILineItem> items, PayTraceBasicAmountTypeFactory factory, bool areDiscounts = false)
        {
            return items.ToArray().Select(
                item => item.ExtendedData.ContainsProductKey() ? 
                    this.BuildProductPaymentDetailsItemType(item, factory) : 
                    this.BuildGenericPaymentDetailsItemType(item, factory, areDiscounts)).ToList();
        }

        /// <summary>
        /// Builds a <see cref="PaymentDetailsItemType"/>.
        /// </summary>
        /// <param name="item">
        /// The item.
        /// </param>
        /// <param name="factory">
        /// The <see cref="PayTraceBasicAmountTypeFactory"/>.
        /// </param>
        /// <param name="isDiscount">
        /// The is discount.
        /// </param>
        /// <returns>
        /// The <see cref="PaymentDetailsItemType"/>.
        /// </returns>
        protected virtual PaymentDetailsItemType BuildGenericPaymentDetailsItemType(ILineItem item, PayTraceBasicAmountTypeFactory factory, bool isDiscount)
        {
            var detailsItemType = new PaymentDetailsItemType
            {
                Name = item.Name,
                ItemURL = null,
                Amount = factory.Build(isDiscount ? -1 * item.Price : item.Price),
                Quantity = item.Quantity
            };

            return detailsItemType;
        }

        /// <summary>
        /// The build product payment details item type.
        /// </summary>
        /// <param name="item">
        /// The item.
        /// </param>        
        /// <param name="factory">
        /// The <see cref="PayTraceBasicAmountTypeFactory"/>.
        /// </param>
        /// <returns>
        /// The <see cref="PaymentDetailsItemType"/>.
        /// </returns>
        protected virtual PaymentDetailsItemType BuildProductPaymentDetailsItemType(ILineItem item, PayTraceBasicAmountTypeFactory factory)
        {
            IProductContent product = null;
            if (_settings.UsesProductContent)
            {
                var productKey = item.ExtendedData.GetProductKey();
                product = _merchello.TypedProductContent(productKey);
            }

            var detailsItemType = new PaymentDetailsItemType
            {
                Name = item.Name,
                ItemURL = product != null ? 
                    string.Format("{0}{1}", _settings.WebsiteUrl, product.Url) :
                    null,
                Amount = factory.Build(item.Price),
                Quantity = item.Quantity
            };

            return detailsItemType;
        }
    }
}