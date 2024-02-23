using Azure.Messaging.ServiceBus;
using Mando.Services.EmailAPI.Message;
using Mango.Services.EmailAPI.Models.Dto;
using Mango.Services.EmailAPI.Services;
using Newtonsoft.Json;
using System.Text;

namespace Mango.Services.EmailAPI.Messaging
{
	public class AzureServiceBusConsumer : IAzureServiceBusConsumer
	{
		private readonly IConfiguration _configuration;
		private readonly EmailService _emailService;
		private readonly string serviceBusConnectionString;
		private readonly string emailCartQueue;
		private readonly string emailNewUserQueue;
		private readonly string orderCreated_Topic;
		private readonly string orderCreated_Email_Subscription;
		private ServiceBusProcessor _emailOrderPlacedProcessor;
		private ServiceBusProcessor _emailCartProcessor;
		private ServiceBusProcessor _emailNewUserProcessor;

		public AzureServiceBusConsumer(IConfiguration configuration, EmailService emailService)
        {
			_configuration = configuration;
			_emailService = emailService;
			serviceBusConnectionString = _configuration.GetValue<string>("ServiceBusConnectionString");
			
			emailCartQueue = _configuration.GetValue<string>("TopicAndQueueNames:EmailShoppingCartQueue");
			emailNewUserQueue = _configuration.GetValue<string>("TopicAndQueueNames:EmailNewUsersQueue");

			orderCreated_Topic = _configuration.GetValue<string>("TopicAndQueueNames:OrderCreatedTopic");
			orderCreated_Email_Subscription = _configuration.GetValue<string>("TopicAndQueueNames:OrderCreated_Email_Subscription");

			var client = new ServiceBusClient(serviceBusConnectionString);
			_emailCartProcessor = client.CreateProcessor(emailCartQueue);
			_emailNewUserProcessor = client.CreateProcessor(emailNewUserQueue);
			_emailOrderPlacedProcessor = client.CreateProcessor(orderCreated_Topic, orderCreated_Email_Subscription);
		}

		public async Task Start()
		{
			_emailCartProcessor.ProcessMessageAsync += OnEmailCartRequestReceived;
			_emailCartProcessor.ProcessErrorAsync += ErrorHandler;
			await _emailCartProcessor.StartProcessingAsync();

			_emailNewUserProcessor.ProcessMessageAsync += OnEmailNewUserReceived;
			_emailNewUserProcessor.ProcessErrorAsync += ErrorHandler;
			await _emailNewUserProcessor.StartProcessingAsync();

			_emailOrderPlacedProcessor.ProcessMessageAsync += OnOrderPlacedReceived;
			_emailOrderPlacedProcessor.ProcessErrorAsync += ErrorHandler;
			await _emailOrderPlacedProcessor.StartProcessingAsync();
		}
		
		public async Task Stop()
		{
			await _emailCartProcessor.StopProcessingAsync();
			await _emailCartProcessor.DisposeAsync();

			await _emailNewUserProcessor.StopProcessingAsync();
			await _emailNewUserProcessor.DisposeAsync();

			await _emailOrderPlacedProcessor.StopProcessingAsync();
			await _emailOrderPlacedProcessor.DisposeAsync();
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
				await _emailService.EmailCartAndLog(cart);

				//This is where the app asks for the ServiceBus to drop out the message from the Queue
				await args.CompleteMessageAsync(args.Message);  
			}
			catch (Exception ex)
			{

				throw;
			}
		}

		private async Task OnEmailNewUserReceived(ProcessMessageEventArgs args)
		{
			//this is where you will receive the message
			var message = args.Message;
			var body = Encoding.UTF8.GetString(message.Body);

			string email = JsonConvert.DeserializeObject<string>(body);

			try
			{
				//TODO - try to log email
				await _emailService.EmailNewUserAndLog(email);

				//This is where the app asks for the ServiceBus to drop out the message from the Queue
				await args.CompleteMessageAsync(args.Message);
			}
			catch (Exception ex)
			{

				throw;
			}
		}

		private async Task OnOrderPlacedReceived(ProcessMessageEventArgs args)
		{
			//this is where you will receive the message
			var message = args.Message;
			var body = Encoding.UTF8.GetString(message.Body);

			RewardsMessage reward = JsonConvert.DeserializeObject<RewardsMessage>(body);

			try
			{
				//TODO - try to log email
				await _emailService.LogOrderPlaced(reward);

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
