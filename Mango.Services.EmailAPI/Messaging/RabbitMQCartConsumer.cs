
using Mango.Services.EmailAPI.Models.Dto;
using Mango.Services.EmailAPI.Services;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Mango.Services.EmailAPI.Messaging
{
	public class RabbitMQCartConsumer : BackgroundService
	{

		private readonly IConfiguration _configuration;
		private readonly EmailService _emailService;
		private readonly string? emailCartQueue;
		private readonly string? registerUserQueue;
		private readonly IConnection _connection;
		private readonly IModel _channel;

		public RabbitMQCartConsumer(IConfiguration configuration, EmailService emailService)
		{
			_configuration = configuration;
			_emailService = emailService;

			emailCartQueue = _configuration.GetValue<string>("TopicAndQueueNames:EmailShoppingCartQueue");

			var factory = new ConnectionFactory
			{
				HostName = "localhost",
				UserName = "guest",
				Password = "guest"
			};
			_connection = factory.CreateConnection();
			_channel = _connection.CreateModel();
			_channel.QueueDeclare(emailCartQueue, false, false, false, null);
		}

		protected override Task ExecuteAsync(CancellationToken stoppingToken)
		{
			stoppingToken.ThrowIfCancellationRequested();

			var consumer = new EventingBasicConsumer(_channel);
			consumer.Received += (ch, ea) =>
			{
				var content = Encoding.UTF8.GetString(ea.Body.ToArray());
				CartDto cartDto = JsonConvert.DeserializeObject<CartDto>(content);
				HandleMessage(cartDto).GetAwaiter().GetResult();
				_channel.BasicAck(ea.DeliveryTag, false);
			};

			_channel.BasicConsume(emailCartQueue, false, consumer);

			return Task.CompletedTask;
		}

		private async Task HandleMessage(CartDto cartDto) 
		{ 
			_emailService.EmailCartAndLog(cartDto).GetAwaiter().GetResult();
		}
	}
}
