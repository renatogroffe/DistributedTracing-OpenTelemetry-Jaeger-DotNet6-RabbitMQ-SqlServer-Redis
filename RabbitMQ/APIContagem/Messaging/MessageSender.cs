using System.Diagnostics;
using System.Text;
using System.Text.Json;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using RabbitMQ.Client;
using APIContagem.Tracing;

namespace APIContagem.Messaging;

public class MessageSender
{
    private static readonly TextMapPropagator Propagator = Propagators.DefaultTextMapPropagator;
    private readonly ILogger<MessageSender> _logger;
    private readonly IConfiguration _configuration;

    public MessageSender(ILogger<MessageSender> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public void SendMessage<T>(T message)
    {
        // Solução que serviu de base para a implementação deste exemplo:
        // https://github.com/open-telemetry/opentelemetry-dotnet/tree/main/examples/MicroserviceExample

        var queueName = _configuration["RabbitMQ:Queue"];
        var exchangeName = _configuration["RabbitMQ:Exchange"];
        var bodyContent = JsonSerializer.Serialize(message);

        try
        {
            var factory = new ConnectionFactory()
            {
                Uri = new Uri(_configuration["RabbitMQ:ConnectionString"])
            };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            // Semantic convention - OpenTelemetry messaging specification:
            // https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/trace/semantic_conventions/messaging.md#span-name
            var activityName = $"{queueName} send";

            using var activity = OpenTelemetryExtensions.CreateActivitySource()
                .StartActivity(activityName, ActivityKind.Producer);
            var props = channel.CreateBasicProperties();

            ActivityContext contextToInject = default;
            if (activity != null)
            {
                contextToInject = activity.Context;
            }
            else if (Activity.Current != null)
            {
                contextToInject = Activity.Current.Context;
            }

            Propagator.Inject(new PropagationContext(contextToInject, Baggage.Current), props,
                InjectTraceContextIntoBasicProperties);

            activity?.SetTag("messaging.system", "rabbitmq");
            activity?.SetTag("messaging.destination_kind", "queue");
            activity?.SetTag("messaging.destination", exchangeName);
            activity?.SetTag("messaging.rabbitmq.routing_key", queueName);
            activity?.SetTag("message", bodyContent);
                
            channel.BasicPublish(exchange: exchangeName,
                                 routingKey: queueName,
                                 basicProperties: props,
                                 body: Encoding.UTF8.GetBytes(bodyContent));

            _logger.LogInformation(
                $"RabbitMQ - Envio para a fila {queueName} concluído | " +
                $"{bodyContent}");
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Message publishing failed.");
            throw;
        }
    }

    private void InjectTraceContextIntoBasicProperties(IBasicProperties props, string key, string value)
    {
        try
        {
            if (props.Headers == null)
            {
                props.Headers = new Dictionary<string, object>();
            }

            props.Headers[key] = value;
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to inject trace context.");
        }
    }
}
