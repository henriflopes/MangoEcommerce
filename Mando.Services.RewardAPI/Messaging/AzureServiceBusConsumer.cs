using Azure.Messaging.ServiceBus;
using Mando.Services.RewardAPI.Message;
using Mango.Services.RewardAPI.Services;
using Newtonsoft.Json;
using System.Text;

namespace Mango.Services.RewardAPI.Messaging
{
	public class AzureServiceBusConsumer : IAzureServiceBusConsumer
	{
		private readonly IConfiguration _configuration;
		private readonly RewardService _rewardService;
		private readonly string serviceBusConnectionString;
		private readonly string orderCreatedTopic;
		private readonly string orderCreatedRewardSubscription;

		private ServiceBusProcessor _rewardProcessor;

		public AzureServiceBusConsumer(IConfiguration configuration, RewardService rewardService)
		{
			_configuration = configuration;
			_rewardService = rewardService;

			serviceBusConnectionString = _configuration.GetValue<string>("ServiceBusConnectionString");

			orderCreatedTopic = _configuration.GetValue<string>("TopicAndQueueNames:OrderCreatedTopic");
			orderCreatedRewardSubscription = _configuration.GetValue<string>("TopicAndQueueNames:OrderCreated_Rewards_Subscription");

			var client = new ServiceBusClient(serviceBusConnectionString);
			_rewardProcessor = client.CreateProcessor(orderCreatedTopic, orderCreatedRewardSubscription);
		}

		public async Task Start()
		{
			_rewardProcessor.ProcessMessageAsync += OnNewOrderRewardsRequestReceived;
			_rewardProcessor.ProcessErrorAsync += ErrorHandler;
			await _rewardProcessor.StartProcessingAsync();
		}

		public async Task Stop()
		{
			await _rewardProcessor.StopProcessingAsync();
			await _rewardProcessor.DisposeAsync();
		}

		private Task ErrorHandler(ProcessErrorEventArgs args)
		{
			Console.WriteLine(args.Exception.ToString());
			return Task.CompletedTask;
		}

		private async Task OnNewOrderRewardsRequestReceived(ProcessMessageEventArgs args)
		{
			//this is where you will receive the message
			var message = args.Message;
			var body = Encoding.UTF8.GetString(message.Body);

			RewardsMessage rewardsMessage = JsonConvert.DeserializeObject<RewardsMessage>(body);

			try
			{
				//TODO - try to log email
				await _rewardService.UpdateRewards(rewardsMessage);

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
