using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace AdvancedMicroservicesSolution.src.ProductService.Services
{
    public class RabbitOptions { public string Host { get; set; } = "localhost"; public string Exchange { get; set; } = "advanced.exchange"; public string User { get; set; } = "guest"; public string Password { get; set; } = "guest"; }
    public class RabbitMqConsumer
    {
    }

    public class RabbitBackgroundService : BackgroundService
    {
        private readonly RabbitMqConsumer _consumer;
        public RabbitBackgroundService(RabbitMqConsumer consumer) => _consumer = consumer;
        protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;
    }
}
