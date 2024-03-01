
using Mando.Services.RewardAPI.Message;
using Mango.Services.RewardAPI.Services;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Mango.Services.RewardAPI.Messaging
{
	public class RabbitMQOrderConsumer : BackgroundService
	{

		private readonly IConfiguration _configuration;
		private readonly RewardService _rewardService;
		private readonly IConnection _connection;
		private readonly IModel _channel;
		private const string _orderCreated_RewardsUpdateQueue = "RewardsUpdateQueue";
		private string _exchangeName = "";

		public RabbitMQOrderConsumer(IConfiguration configuration, RewardService rewardService)
		{
			_configuration = configuration;
			_rewardService = rewardService;

			_exchangeName = _configuration.GetValue<string>("TopicAndQueueNames:OrderCreatedTopic");

			var factory = new ConnectionFactory
			{
				HostName = "localhost",
				UserName = "guest",
				Password = "guest"
			};
			_connection = factory.CreateConnection();
			_channel = _connection.CreateModel();
			_channel.ExchangeDeclare(_exchangeName, ExchangeType.Direct);
			_channel.QueueDeclare(_orderCreated_RewardsUpdateQueue, false, false, false, null);
			_channel.QueueBind(_orderCreated_RewardsUpdateQueue, _exchangeName, "RewardsUpdate");
		}

		protected override Task ExecuteAsync(CancellationToken stoppingToken)
		{
			stoppingToken.ThrowIfCancellationRequested();

			var consumer = new EventingBasicConsumer(_channel);
			consumer.Received += (ch, ea) =>
			{
				var content = Encoding.UTF8.GetString(ea.Body.ToArray());
				RewardsMessage rewardsMessage = JsonConvert.DeserializeObject<RewardsMessage>(content);
				HandleMessage(rewardsMessage).GetAwaiter().GetResult();
				_channel.BasicAck(ea.DeliveryTag, false);
			};

			_channel.BasicConsume(_orderCreated_RewardsUpdateQueue, false, consumer);

			return Task.CompletedTask;
		}

		private async Task HandleMessage(RewardsMessage rewardsMessage) 
		{
			_rewardService.UpdateRewards(rewardsMessage).GetAwaiter().GetResult();
		}
	}
}
