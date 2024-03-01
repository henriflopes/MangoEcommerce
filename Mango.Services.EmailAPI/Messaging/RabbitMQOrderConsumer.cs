﻿
using Mando.Services.EmailAPI.Message;
using Mango.Services.EmailAPI.Models.Dto;
using Mango.Services.EmailAPI.Services;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Mango.Services.EmailAPI.Messaging
{
	public class RabbitMQOrderConsumer : BackgroundService
	{

		private readonly IConfiguration _configuration;
		private readonly EmailService _emailService;
		private readonly IConnection _connection;
		private readonly IModel _channel;
		private const string _orderCreated_EmailUpdateQueue = "EmailUpdateQueue";
		private string _exchangeName = "";

		public RabbitMQOrderConsumer(IConfiguration configuration, EmailService emailService)
		{
			_configuration = configuration;
			_emailService = emailService;

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
			_channel.QueueDeclare(_orderCreated_EmailUpdateQueue, false, false, false, null);
			_channel.QueueBind(_orderCreated_EmailUpdateQueue, _exchangeName, "EmailUpdate");
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

			_channel.BasicConsume(_orderCreated_EmailUpdateQueue, false, consumer);

			return Task.CompletedTask;
		}

		private async Task HandleMessage(RewardsMessage rewardsMessage) 
		{ 
			_emailService.LogOrderPlaced(rewardsMessage).GetAwaiter().GetResult();
		}
	}
}
