﻿namespace Merchello.Core.Builders
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using Merchello.Core.Checkout;
    using Merchello.Core.Models;

    using Umbraco.Core;

    /// <summary>
    /// A builder chain used by the checkout manager to create invoices.
    /// </summary>
    /// <remarks>
    /// Supersedes the <see cref="InvoiceBuilderChain"/>
    /// </remarks>
    internal sealed class CheckoutInvoiceBuilderChain : BuildChainBase<IInvoice>
    {
        /// <summary>
        /// Gets the <see cref="CheckoutManagerBase"/>.
        /// </summary>
        private readonly ICheckoutManagerBase _checkoutManager;

        /// <summary>
        /// Constructor parameters for the base class activator
        /// </summary>
        private IEnumerable<object> _constructorParameters;

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckoutInvoiceBuilderChain"/> class.
        /// </summary>
        /// <param name="checkoutManager">
        /// The checkout manager.
        /// </param>
        internal CheckoutInvoiceBuilderChain(ICheckoutManagerBase checkoutManager)
        {
            Mandate.ParameterNotNull(checkoutManager, "checkoutManager");

            _checkoutManager = checkoutManager;

            ResolveChain(Core.Constants.TaskChainAlias.CheckoutManagerInvoiceCreate);
        }

        /// <summary>
        /// Gets the count of tasks - Used for testing
        /// </summary>
        internal int TaskCount
        {
            get { return TaskHandlers.Count(); }
        }

        /// <summary>
        /// Gets the constructor argument values.
        /// </summary>
        protected override IEnumerable<object> ConstructorArgumentValues
        {
            get
            {
                return _constructorParameters ??
                    (_constructorParameters = new List<object>(new object[] { _checkoutManager }));
            }
        }

        /// <summary>
        /// Builds the invoice
        /// </summary>
        /// <returns>The Attempt{IInvoice} representing the successful creation of an invoice</returns>
        public override Attempt<IInvoice> Build()
        {
            var unpaid =
                _checkoutManager.Context.Services.InvoiceService.GetInvoiceStatusByKey(Core.Constants.DefaultKeys.InvoiceStatus.Unpaid);

            if (unpaid == null)
                return Attempt<IInvoice>.Fail(new NullReferenceException("Unpaid invoice status query returned null"));

			//// S6 Implemented manually after review with Rusty 2.21.17
			//// Invoice needs to be created via the service so that the Creating / Created events get fired.
			//// see http://issues.merchello.com/youtrack/issue/M-1290
			var invoice = _checkoutManager.Context.Services.InvoiceService.CreateInvoice(unpaid.Key);
			invoice.VersionKey = _checkoutManager.Context.VersionKey;

			////var invoice = new Invoice(unpaid) { VersionKey = _checkoutManager.Context.VersionKey };

			// Associate a customer with the invoice if it is a known customer.
			if (!_checkoutManager.Context.Customer.IsAnonymous) invoice.CustomerKey = _checkoutManager.Context.Customer.Key;

            var attempt = TaskHandlers.Any()
                       ? TaskHandlers.First().Execute(invoice)
                       : Attempt<IInvoice>.Fail(new InvalidOperationException("The configuration Chain Task List could not be instantiated"));

            if (!attempt.Success) return attempt;

			// S6 We are adding rounding here to each TotalPrice otherwise the sum calculation is sometimes off by a penny
			//var charges = attempt.Result.Items.Where(x => x.LineItemType != LineItemType.Discount).Sum(x => x.TotalPrice);
			//var discounts = attempt.Result.Items.Where(x => x.LineItemType == LineItemType.Discount).Sum(x => x.TotalPrice);
			var charges = attempt.Result.Items.Where(x => x.LineItemType != LineItemType.Discount).Sum(x => Math.Round(x.TotalPrice, 2, MidpointRounding.AwayFromZero));
            var discounts = attempt.Result.Items.Where(x => x.LineItemType == LineItemType.Discount).Sum(x => Math.Round(x.TotalPrice, 2, MidpointRounding.AwayFromZero));

            // total the invoice
            decimal converted;

			// S6 Implement Midpoint rounding instead of C# default bankers rounding http://stackoverflow.com/questions/977796/why-does-math-round2-5-return-2-instead-of-3
			//attempt.Result.Total = Math.Round(decimal.TryParse((charges - discounts).ToString(CultureInfo.InvariantCulture), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture.NumberFormat, out converted) ? converted : 0, 2);
			attempt.Result.Total = Math.Round(decimal.TryParse((charges - discounts).ToString(CultureInfo.InvariantCulture), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture.NumberFormat, out converted) ? converted : 0, 2, MidpointRounding.AwayFromZero);

			return attempt;
        }
    }
}