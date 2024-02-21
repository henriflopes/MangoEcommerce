using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using System.Text;

namespace Mango.MessageBus
{
	public class MessageBus : IMessageBus
	{

		private string connectionString = "Endpoint=sb://mangowebsb.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=QvjQiimzro4ecXhYTVdW7C4Mc/Gc4RtUu+ASbCUwlKk=";


		public MessageBus()
		{

		}

		public async Task PublishMessage(object message, string topicQueueName)
		{
			await using var client = new ServiceBusClient(connectionString);

			ServiceBusSender sender = client.CreateSender(topicQueueName);

			var jsonMessage = JsonConvert.SerializeObject(message);
			ServiceBusMessage finalMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(jsonMessage))
			{
				CorrelationId = Guid.NewGuid().ToString()
			};

			await sender.SendMessageAsync(finalMessage);
			await client.DisposeAsync();
		}
	}
}
