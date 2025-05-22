using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.Metrics;
using System.Diagnostics;

namespace OTELAWS.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private static readonly ActivitySource ActivitySource = new ActivitySource("MyCompany.WeatherForecast");
        private static readonly Meter Meter = new Meter("MyCompany.WeatherForecast.Metrics");

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly Counter<long> _requestCounter;

        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger;
            _requestCounter = Meter.CreateCounter<long>("weather_forecast_requests_total");
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            using var activity = ActivitySource.StartActivity("WeatherForecast.Get");
            activity?.SetTag("http.method", "GET");
            activity?.SetTag("http.route", "/WeatherForecast");

            _logger.LogInformation("WeatherForecast GET endpoint called");

            _requestCounter.Add(1);

            var result = Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();

            activity?.SetTag("response.count", result.Length);

            return result;
        }

        [HttpPost("{city}")]
        public IActionResult PostWeatherReport(string city, [FromBody] WeatherForecast forecast)
        {
            using var activity = ActivitySource.StartActivity("WeatherForecast.Post");
            activity?.SetTag("http.method", "POST");
            activity?.SetTag("http.route", $"/WeatherForecast/{city}");
            activity?.SetTag("city", city);

            _logger.LogInformation("Received weather report for city {City}", city);

            _requestCounter.Add(1);

            // Basit validation
            if (forecast == null)
            {
                activity?.SetStatus(ActivityStatusCode.Error, "Forecast body is null");
                _logger.LogWarning("Forecast body was null");
                return BadRequest("Forecast body is required");
            }

            
            activity?.SetTag("forecast.temperatureC", forecast.TemperatureC);
            activity?.SetTag("forecast.summary", forecast.Summary);

            return Ok(new
            {
                Message = $"Weather report for {city} received",
                Forecast = forecast
            });
        }
    }
}
