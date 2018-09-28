namespace Merchello.Providers.Payment.PayTrace
{
	using System;
	using System.Linq;

	using Merchello.Core.Models;
	using Merchello.Core.Services;
	using Merchello.Providers.Payment.Models;

	using Umbraco.Core;
	using Umbraco.Core.Events;
	using Umbraco.Core.Logging;

	using Constants = Providers.Constants;
	using Models;

	public class PayTraceEvents : ApplicationEventHandler
	{
		protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication,
										   ApplicationContext applicationContext)
		{
			base.ApplicationStarted(umbracoApplication, applicationContext);

			LogHelper.Info<PayTraceEvents>("Initializing PayTrace provider registration binding events");


			GatewayProviderService.Saving += delegate(IGatewayProviderService sender, SaveEventArgs<IGatewayProviderSettings> args)
			{
				var key = new Guid(Constants.PayTrace.GatewayProviderKey);
				var provider = args.SavedEntities.FirstOrDefault(x => key == x.Key && !x.HasIdentity);
				if (provider == null) return;

				provider.ExtendedData.SaveProviderSettings(new PayTraceProviderSettings());

			};
		}
	}
}
