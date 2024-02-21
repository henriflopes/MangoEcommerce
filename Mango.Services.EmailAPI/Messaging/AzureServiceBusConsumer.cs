using Azure.Messaging.ServiceBus;
using Mango.Services.EmailAPI.Models.Dto;
using Newtonsoft.Json;
using System.Text;

namespace Mango.Services.EmailAPI.Messaging
{
	public class AzureServiceBusConsumer : IAzureServiceBusConsumer
	{
		private readonly IConfiguration _configuration;
		private readonly string serviceBusConnectionString;
		private readonly string emailCartQueue;

		private ServiceBusProcessor _emailCartProcessor;

		public AzureServiceBusConsumer(IConfiguration configuration)
        {
			_configuration = configuration;
			serviceBusConnectionString = _configuration.GetValue<string>("ServiceBusConnectionString");
			emailCartQueue = _configuration.GetValue<string>("TopicAndQueueNames:EmailShoppingCartQueue");

			var client = new ServiceBusClient(serviceBusConnectionString);
			_emailCartProcessor = client.CreateProcessor(emailCartQueue);
		}

		public async Task Start()
		{
			_emailCartProcessor.ProcessMessageAsync += OnEmailCartRequestReceived;
			_emailCartProcessor.ProcessErrorAsync += ErrorHandler;
			await _emailCartProcessor.StartProcessingAsync();
		}
		
		public async Task Stop()
		{
			await _emailCartProcessor.StopProcessingAsync();
			await _emailCartProcessor.DisposeAsync();
		}

		private Task ErrorHandler(ProcessErrorEventArgs args)
		{
			Console.WriteLine(args.Exception.ToString());
			return Task.CompletedTask;
		}

		private async Task OnEmailCartRequestReceived(ProcessMessageEventArgs args)
		{
			//this is where you will receive the message
			var message = args.Message;
			var body = Encoding.UTF8.GetString(message.Body);

			CartDto cart = JsonConvert.DeserializeObject<CartDto>(body);

			try
			{
				//TODO - try to log email
				//This is where the app asks for the ServiceBus to drop out the message from the Queue
				await args.CompleteMessageAsync(args.Message);  

			}
			catch (Exception ex)
			{

				throw;
			}

		}

		
	}
}
