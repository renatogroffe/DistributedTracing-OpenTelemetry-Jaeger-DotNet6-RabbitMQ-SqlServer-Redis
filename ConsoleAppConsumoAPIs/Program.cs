using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using ConsoleAppConsumoAPIs;

Console.WriteLine("***** Tracing Distribuído com .NET 6 + ASP.NET Core + Jaeger + OpenTelemetry + " +
    "RabbitMQ + SQL Server + Redis *****");

using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource(nameof(ConsoleAppConsumoAPIs))
    .SetResourceBuilder(
        ResourceBuilder.CreateDefault()
            .AddService(serviceName: nameof(ConsoleAppConsumoAPIs), serviceVersion: "1.0.0"))
    .AddHttpClientInstrumentation()
    .AddJaegerExporter(exporter =>
    {
        exporter.AgentHost = "localhost";
        exporter.AgentPort = 6831;
    })
    .Build();

using var client = new HttpClient();
using var activitySource = new ActivitySource(nameof(ConsoleAppConsumoAPIs), "1.0.0");

while (true)
{
    using var activity = activitySource.StartActivity("SendRequests");
    activity?.SetTag("startPoint", "Program.cs");

    Endpoints.SendRequest("RabbitMQ", Endpoints.APIContagemRabbitMQ, client);
    Endpoints.SendRequest("Redis", Endpoints.APIContagemRedis, client);

    activity?.Dispose();

    Console.WriteLine("Pressione ENTER para enviar uma nova requisição...");
    Console.ReadLine();
}