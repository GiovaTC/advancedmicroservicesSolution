using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace AdvancedMicroservicesSolution.src.ApiGateway.Messaging
{
    public class RabbitMqPublisher : IDisposable
    {
        private readonly IConnection _connection;
        private readonly RabbitMQ.Client.IModel _channel;
        private readonly IConfiguration _config;

        public RabbitMqPublisher(IConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            var host = _config["RabbitMQ:Host"] ?? throw new ArgumentNullException("RabbitMQ:Host");
            var user = _config["RabbitMQ:User"] ?? throw new ArgumentNullException("RabbitMQ:User");
            var pass = _config["RabbitMQ:Password"] ?? throw new ArgumentNullException("RabbitMQ:Password");
            var exchange = _config["RabbitMQ:Exchange"] ?? throw new ArgumentNullException("RabbitMQ:Exchange");

            var factory = new ConnectionFactory
            {
                HostName = host,
                UserName = user,
                Password = pass
            };

            _connection = factory.CreateConnection(); // ✅ usa la versión síncrona
            _channel = _connection.CreateModel()
                ?? throw new InvalidOperationException("No se pudo crear el canal RabbitMQ.");

            _channel.ExchangeDeclare(exchange, ExchangeType.Fanout, durable: true);
        }

        public void Publish<T>(T message, string routingKey = "")
        {
            var exchange = _config["RabbitMQ:Exchange"] ?? throw new ArgumentNullException("RabbitMQ:Exchange");
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            _channel.BasicPublish(
                exchange: exchange,
                routingKey: routingKey,
                basicProperties: null,
                body: body
            );
        }

        public void Dispose()
        {
            if (_channel?.IsOpen == true)
                _channel.Close();
            if (_connection?.IsOpen == true)
                _connection.Close();
            GC.SuppressFinalize(this);
        }
    }
}
