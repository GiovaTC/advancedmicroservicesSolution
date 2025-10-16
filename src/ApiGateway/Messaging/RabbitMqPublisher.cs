using RabbitMQ.Client;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace AdvancedMicroservicesSolution.src.ApiGateway.Messaging
{
    public class RabbitMqPublisher : IDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
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

            // Nueva API (asincrónica) en RabbitMQ.Client 7+
            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            _channel.ExchangeDeclareAsync(exchange, ExchangeType.Fanout, durable: true).GetAwaiter().GetResult();
        }

        public void Publish<T>(T message, string routingKey = "")
        {
            var exchange = _config["RabbitMQ:Exchange"] ?? throw new ArgumentNullException("RabbitMQ:Exchange");
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            _channel.BasicPublishAsync(exchange, routingKey, body).GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
