using Microsoft.AspNetCore.Mvc;
using APIContagem.Models;
using APIContagem.Messaging;
using APIContagem.Tracing;

namespace APIContagem.Controllers;

[ApiController]
[Route("[controller]")]
public class ContadorController : ControllerBase
{
    private static readonly Contador _CONTADOR = new Contador();
    private readonly ILogger<ContadorController> _logger;
    private readonly IConfiguration _configuration;
    private readonly MessageSender _messageSender;

    public ContadorController(ILogger<ContadorController> logger,
        IConfiguration configuration, MessageSender messageSender)
    {
        _logger = logger;
        _configuration = configuration;
        _messageSender = messageSender;
    }

    [HttpGet]
    public ResultadoContador Get()
    {
        _logger.LogInformation("Gerando valor...");
        
        int valorAtualContador;
        lock (_CONTADOR)
        {
            _CONTADOR.Incrementar();
            valorAtualContador = _CONTADOR.ValorAtual;
        }

        using var activity = OpenTelemetryExtensions.CreateActivitySource()
            .StartActivity("Identificando");
        activity?.SetTag("valorAtual", valorAtualContador);

        var resultado = new ResultadoContador()
        {
            ValorAtual = valorAtualContador,
            Producer = _CONTADOR.Local,
            Kernel = _CONTADOR.Kernel,
            Framework = _CONTADOR.Framework,
            Mensagem = _configuration["MensagemVariavel"]
        };

        _logger.LogInformation("Iniciando envio da mensagem...");
        _messageSender.SendMessage<ResultadoContador>(resultado);

        return resultado;
    }
}