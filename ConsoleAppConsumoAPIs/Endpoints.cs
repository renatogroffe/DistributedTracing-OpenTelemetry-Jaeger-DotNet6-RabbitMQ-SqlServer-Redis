using System.Diagnostics;
using ConsoleAppConsumoAPIs.Models;
using System.Net.Http.Json;

namespace ConsoleAppConsumoAPIs;

public static class Endpoints
{
    public static string APIContagemRabbitMQ { get; } = "http://localhost:6200/contador";
    public static string APIContagemRedis { get; } = "http://localhost:6300/contador";

    public static void SendRequest(string tecnologia, string endpoint, HttpClient client)
    {
        using var activity = new ActivitySource(nameof(ConsoleAppConsumoAPIs), "1.0.0")
            .StartActivity($"{nameof(SendRequest)} APIContagem{tecnologia}");
        activity?.SetTag("endpoint", endpoint);

        var resultado = client.GetFromJsonAsync<ResultadoContador>(endpoint).Result;
        activity?.SetTag("valorAtual", resultado!.ValorAtual);

        Console.WriteLine(
            $"{tecnologia}: {resultado!.Producer} | Valor atual = {resultado.ValorAtual}");
    }
}