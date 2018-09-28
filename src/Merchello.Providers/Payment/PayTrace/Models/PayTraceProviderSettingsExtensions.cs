﻿namespace Merchello.Providers.Payment.PayTrace.Models
{
	using System.Diagnostics.CodeAnalysis;

	using Merchello.Providers.Exceptions;

	using Umbraco.Core;

	/// <summary>
	/// Extension methods for <see cref="PayTraceProviderSettings"/>.
	/// </summary>
	[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed. Suppression is OK here.")]
	public static class PayTraceProviderSettingsExtensions
	{
		/// <summary>
		/// Attempts to get the SDK configuration.
		/// </summary>
		/// <param name="settings">
		/// The settings.
		/// </param>
		/// <returns>
		/// The <see cref="SdkConfig"/>.
		/// </returns>
		public static Attempt<SdkConfig> GetApiSdkConfig(this PayTraceProviderSettings settings)
		{
			var isReady = ValidatePayTraceProviderSettings(settings);

			return isReady.Success
					   ? Attempt<SdkConfig>.Succeed(
						   new SdkConfig
							{
								{ "mode", settings.Mode.ToString().ToLowerInvariant() }
							})
					   : Attempt<SdkConfig>.Fail(isReady.Exception);
		}

		/// <summary>
		/// Attempts to get the SdkConfig for PayTrace express checkout.
		/// </summary>
		/// <param name="settings">
		/// The settings.
		/// </param>
		/// <returns>
		/// The <see cref="Attempt"/>.
		/// </returns>
		public static Attempt<SdkConfig> GetCheckoutSdkConfig(this PayTraceProviderSettings settings)
		{
			var isReady = ValidatePayTraceProviderSettings(settings);

			return isReady.Success
					   ? Attempt<SdkConfig>.Succeed(
						   new SdkConfig
							   {
									{ "mode", settings.Mode.ToString().ToLowerInvariant() },
									{ "account1.apiUsername", settings.ApiUsername },
									{ "account1.apiPassword", settings.ApiPassword },
									{ "account1.apiSignature", settings.ApiSignature }
							   })

					   : Attempt<SdkConfig>.Fail(isReady.Exception);
		}

		/// <summary>
		/// Validates credentials have been entered in the back office.
		/// </summary>
		/// <param name="settings">
		/// The settings.
		/// </param>
		/// <param name="isExpress">
		/// The is express.
		/// </param>
		/// <returns>
		/// The <see cref="Attempt"/>.
		/// </returns>
		private static Attempt<bool> ValidatePayTraceProviderSettings(PayTraceProviderSettings settings)
		{
			bool success = !(settings.ApiUsername.IsNullOrWhiteSpace() ||
			settings.ApiPassword.IsNullOrWhiteSpace() ||
			settings.ApiSignature.IsNullOrWhiteSpace());

				// or 
				// = !(settings.ClientId.IsNullOrWhiteSpace() || settings.ClientSecret.IsNullOrWhiteSpace());
			
			var ex = new PayTraceApiException("One or more of the required API credentials has not been set in the back office");

			return success ? Attempt<bool>.Succeed(true) : Attempt<bool>.Fail(false, ex);
		}
	}
}