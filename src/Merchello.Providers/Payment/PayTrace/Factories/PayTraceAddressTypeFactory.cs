namespace Merchello.Providers.Payment.PayTrace.Factories
{
    using System;

    using Merchello.Core.Models;

    //using global::PayTrace.PayTraceAPIInterfaceService.Model;

    using Merchello.Core.Logging;

    /// <summary>
    /// A factory to build PayTrace <see cref="AddressType"/>.
    /// </summary>
    public class PayTraceAddressTypeFactory
    {
        /// <summary>
        /// Builds the <see cref="AddressType"/>.
        /// </summary>
        /// <param name="address">
        /// The address.
        /// </param>
        /// <returns>
        /// The <see cref="AddressType"/>.
        /// </returns>
        public AddressType Build(IAddress address)
        {
            try
            {
                return new AddressType
                           {
                               Name = address.Name,
                               Street1 = address.Address1,
                               Street2 = address.Address2,
                               PostalCode = address.PostalCode,
                               CityName = address.Locality,
                               StateOrProvince = address.Region,
                               CountryName = address.Country().Name,
                               Country =
                                   (CountryCodeType)
                                   Enum.Parse(typeof(CountryCodeType), address.Country().CountryCode, true),
                               Phone = address.Phone
                           };
            }
            catch (Exception ex)
            {
                var logData = MultiLogger.GetBaseLoggingData();
                logData.AddCategory("PayTrace");

                MultiLogHelper.Error<PayTraceBasicAmountTypeFactory>("Failed to build an AddressType", ex, logData);

                throw;
            }

        } 
    }
}