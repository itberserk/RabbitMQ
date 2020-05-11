using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace Consumer
{
    public class BgConsumer : BackgroundService
    {
        private readonly IConnection _rmqConnection;
        private readonly ILogger<BgConsumer> _logger;
        private readonly IModel _channel;

        public BgConsumer(
            IConnection rmqConnection,
            ILogger<BgConsumer> logger)
        {
            _rmqConnection = rmqConnection;
            _logger = logger;

            _channel = _rmqConnection.CreateModel();
            _channel.ExchangeDeclare("MyExchange", ExchangeType.Topic, true);
            _channel.QueueDeclare("MyQueue", true, false, false, null);
            _channel.QueueBind("MyQueue", "MyExchange", "my.routing.key.*", null);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _channel.BasicQos(0, 100, false);
            var consumer = new EventingBasicConsumer(_channel);

            //while (!stoppingToken.IsCancellationRequested)
            //{
                consumer.Received += (ch, ea) =>
                {
                    var body = ea.Body;

                    HandleMessage(body);

                    _channel.BasicAck(ea.DeliveryTag, false);
                };
            //}

            string consumerTag = _channel.BasicConsume("MyQueue", false, consumer);
        }

        private void HandleMessage(ReadOnlyMemory<byte> message)
        {
            string json = JsonConvert.SerializeObject(message, Formatting.Indented);

            _logger.LogWarning(json);
        }
    }
}
