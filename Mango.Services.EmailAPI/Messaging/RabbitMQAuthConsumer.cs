﻿
using Mango.Services.EmailAPI.Services;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Mango.Services.EmailAPI.Messaging
{
	public class RabbitMQAuthConsumer : BackgroundService
	{

		private readonly IConfiguration _configuration;
		private readonly EmailService _emailService;
		private readonly string? registerUserQueue;
		private readonly IConnection _connection;
		private readonly IModel _channel;

		public RabbitMQAuthConsumer(IConfiguration configuration, EmailService emailService)
		{
			_configuration = configuration;
			_emailService = emailService;

			registerUserQueue = _configuration.GetValue<string>("TopicAndQueueNames:RegisterUserQueue");

			var factory = new ConnectionFactory
			{
				HostName = "localhost",
				UserName = "guest",
				Password = "guest"
			};
			_connection = factory.CreateConnection();
			_channel = _connection.CreateModel();
			_channel.QueueDeclare(registerUserQueue, false, false, false, null);
		}

		protected override Task ExecuteAsync(CancellationToken stoppingToken)
		{
			stoppingToken.ThrowIfCancellationRequested();

			var consumer = new EventingBasicConsumer(_channel);
			consumer.Received += (ch, ea) =>
			{
				var content = Encoding.UTF8.GetString(ea.Body.ToArray());
				String email = JsonConvert.DeserializeObject<string>(content);
				HandleMessage(email).GetAwaiter().GetResult();
				_channel.BasicAck(ea.DeliveryTag, false);
			};

			_channel.BasicConsume(registerUserQueue, false, consumer);

			return Task.CompletedTask;
		}

		private async Task HandleMessage(string email) 
		{ 
			_emailService.EmailNewUserAndLog(email).GetAwaiter().GetResult();
		}
	}
}