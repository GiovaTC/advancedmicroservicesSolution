using RabbitMQ.Client;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace AdvancedMicroservicesSolution.src.ApiGateway.Messaging
{
    public class RabbitMqPublisher : IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly IConfiguration _config;

        public RabbitMqPublisher(IConfiguration config)
        {
            _config = config;

            var host = _config["RabbitMQ:Host"] ?? throw new ArgumentNullException("RabbitMQ:Host");
            var user = _config["RabbitMQ:User"] ?? throw new ArgumentNullException("RabbitMQ:User");
            var pass = _config["RabbitMQ:Password"] ?? throw new ArgumentNullException("RabbitMQ:Password");
            var exchange = _config["RabbitMQ:Exchange"] ?? throw new ArgumentNullException("RabbitMQ:Exchange");

            var factory = new ConnectionFactory()
            {
                HostName = host,
                UserName = user,
                Password = pass
            };

            // API síncrona tradicional de RabbitMQ.Client
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.ExchangeDeclare(exchange, ExchangeType.Fanout, durable: true);
        }

        public void Publish<T>(T message, string routingKey = "")
        {
            var exchange = _config["RabbitMQ:Exchange"] ?? throw new ArgumentNullException("RabbitMQ:Exchange");
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            _channel.BasicPublish(exchange, routingKey, null, body);
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
