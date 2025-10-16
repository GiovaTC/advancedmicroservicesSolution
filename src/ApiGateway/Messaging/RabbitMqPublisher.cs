using Microsoft.EntityFrameworkCore.Metadata;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;


namespace AdvancedMicroservicesSolution.src.ApiGateway.Messaging
{
    public class RabbitMqPublisher : IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly IConfiguration _config;
        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
