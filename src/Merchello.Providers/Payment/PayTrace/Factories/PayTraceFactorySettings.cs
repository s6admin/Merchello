namespace Merchello.Providers.Payment.PayTrace.Factories
{
    using Merchello.Core;

    /// <summary>
    /// Setting for the <see cref="PayTracePaymentDetailsTypeFactory"/>.
    /// </summary>
    public class PayTraceFactorySettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PayTraceFactorySettings"/> class.
        /// </summary>
        public PayTraceFactorySettings()
            : this(string.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PayTraceFactorySettings"/> class.
        /// </summary>
        /// <param name="websiteUrl">
        /// The website URL.
        /// </param>
        /// <param name="usesProductContent">
        /// A value indicating that the store is rendering product via IProductContent.
        /// </param>
        public PayTraceFactorySettings(string websiteUrl, bool usesProductContent = true)
        {
            this.UsesProductContent = usesProductContent;
            this.WebsiteUrl = websiteUrl;

            this.Initialize();
        }

        /// <summary>
        /// Gets or sets the website url.
        /// </summary>
        public string WebsiteUrl { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether uses product content.
        /// </summary>
        public bool UsesProductContent { get; set; }

        /// <summary>
        /// Initializes the factory.
        /// </summary>
        private void Initialize()
        {
            if (!this.WebsiteUrl.IsNullOrWhiteSpace()) return;
            this.WebsiteUrl = PayTraceApiHelper.GetBaseWebsiteUrl();

        }
    }
}