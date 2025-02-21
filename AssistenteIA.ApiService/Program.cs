using AssistenteIA.ApiService.Extensions;
using AssistenteIA.ApiService.Models.DTOs;
using AssistenteIA.ApiService.Services;
using AssistenteIA.ServiceDefaults;
using Dapper;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Pgvector.Dapper;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();


builder.Services.AddHealthChecks();

builder.Services.AddHealthChecksUI(options =>
{
    options.SetEvaluationTimeInSeconds(5);
    options.MaximumHistoryEntriesPerEndpoint(10);
    options.AddHealthCheckEndpoint("API de Chat", "/health");
}).AddInMemoryStorage();

// Configurando todas as injeções de depêndencia aqui
builder.Services.ConfigureEmbeddingClient(builder.Configuration);
builder.Services.ConfigureChatClient(builder.Configuration);
builder.Services.ConfigureRepositories();
builder.Services.ConfigureServices();

// Configurando o Dapper para trabalhar com vetores
SqlMapper.AddTypeHandler(new VectorTypeHandler());


var app = builder.Build();

app.UseExceptionHandler();

app.UseHealthChecks("/health", new HealthCheckOptions
{
    Predicate = p => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.UseHealthChecksUI(options => { options.UIPath = "/dashboard"; });


app.MapPost("/chat", async (ChatService service, RAGService rag, [FromBody] MensagemDTO mensagem, CancellationToken cancellationToken) =>
{
    return Results.Ok(await service.ProcessarChat(mensagem.Texto, cancellationToken));
})
.WithName("Chat");

app.MapPost("/treinar-rag", async (RAGService service, [FromBody] List<RAGItemDTO> itens, CancellationToken cancellationToken) =>
{
    await service.TreinarRAG(itens, cancellationToken);
    return Results.Ok();
})
.WithName("Treinar-rag");


app.MapDefaultEndpoints();

app.Run();