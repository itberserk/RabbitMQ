using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace Producer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly IConnection _rmqConnection;

        public WeatherForecastController(IConnection rmqConnection)
        {
            _rmqConnection = rmqConnection;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            var rng = new Random();

            var result = Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            });

            var chanel = _rmqConnection.CreateModel();
            chanel.ExchangeDeclare("MyExchange", ExchangeType.Topic, true);
            chanel.QueueDeclare("MyQueue", false, false, false, null);
            chanel.QueueBind("MyQueue", "MyExchange", "my.routing.key.*", null);


            foreach (var item in result)
            {
                var message = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(item, Formatting.Indented));
                chanel.BasicPublish("MyExchange", "my.routing.key.weather-forecast", null, message);
            }


            return result.ToArray();
        }
    }
}
