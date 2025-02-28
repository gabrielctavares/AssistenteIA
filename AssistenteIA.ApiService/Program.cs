using AssistenteIA.ApiService.Extensions;
using AssistenteIA.ApiService.Models.DTOs;
using AssistenteIA.ApiService.Services;
using AssistenteIA.ServiceDefaults;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Pgvector.Dapper;
using System.Runtime.InteropServices;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.Logging.ClearProviders(); 
builder.Logging.AddConsole(); 
builder.Logging.AddDebug();

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    builder.Logging.AddEventLog();

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

// Configurando todas as injeções de depêndencia aqui
builder.Services.ConfigureEmbeddingClient(builder.Configuration);
builder.Services.ConfigureChatClient(builder.Configuration);
builder.Services.ConfigureRepositories();
builder.Services.ConfigureServices();

// Configurando o Dapper para trabalhar com vetores
SqlMapper.AddTypeHandler(new VectorTypeHandler());


var app = builder.Build();

app.UseExceptionHandler();


app.MapPost("/chat", async ([FromServices] ChatService service, [FromServices] ILogger<Program> logger, [FromBody] MensagemDTO mensagem, CancellationToken cancellationToken = default) =>
{
    if (string.IsNullOrWhiteSpace(mensagem.Texto))
        return Results.BadRequest("Texto não pode ser vazio.");

    try
    {
        var resultado = await service.ProcessarChat(mensagem.Texto, cancellationToken);
        return Results.Ok(resultado);
    }
    catch (OperationCanceledException)
    {
        logger.LogInformation("Operação de chat cancelada pelo cliente.");
        return Results.StatusCode(499);
    }
    catch (Exception e)
	{
        logger.LogError(e, "Erro ao processar chat.");
        return Results.Problem(e.Message);
    }
})
.WithName("Chat");

app.MapPost("/treinar-rag", async ([FromServices] RAGService service, [FromServices] ILogger<Program> logger, [FromBody] List<RAGItemDTO> itens, CancellationToken cancellationToken = default) =>
{
    if (itens == null || itens.Count == 0)
        return Results.BadRequest("A lista de itens para treinamento não pode ser vazia.");

    try
    {
        await service.TreinarRAG(itens, cancellationToken);
        return Results.Ok();
    }
    catch (OperationCanceledException)
    {
        logger.LogInformation("Treinamento RAG cancelado pelo cliente.");
        return Results.StatusCode(499);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erro ao treinar RAG");
        return Results.Problem("Erro interno ao processar o treinamento.");
    }
})
.WithName("Treinar RAG");

app.MapDefaultEndpoints();

app.Run();