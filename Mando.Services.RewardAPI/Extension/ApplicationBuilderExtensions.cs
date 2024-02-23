using Mango.Services.RewardAPI.Messaging;

namespace Mango.Services.RewardAPI.Extension
{
	public static class ApplicationBuilderExtensions
	{
        private static IAzureServiceBusConsumer _serviceBusConsumer { get; set; }


        public static IApplicationBuilder UseAzureServiceBusConsumer(this IApplicationBuilder app)
        {
			_serviceBusConsumer = app.ApplicationServices.GetService<IAzureServiceBusConsumer>();
			var hostApplicationLifer = app.ApplicationServices.GetService<IHostApplicationLifetime>();

			hostApplicationLifer.ApplicationStarted.Register(OnStart);
			hostApplicationLifer.ApplicationStopping.Register(OnStop);

			return app;
		}

		private static void OnStop()
		{
			_serviceBusConsumer.Stop();
		}

		private static void OnStart()
		{
			_serviceBusConsumer.Start();
		}
	}
}
