using AssistenteIA.ApiService.Extensions;
using AssistenteIA.ApiService.Models.DTOs;
using AssistenteIA.ApiService.Services;
using AssistenteIA.ServiceDefaults;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;

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


var app = builder.Build();

app.UseExceptionHandler();

app.UseHealthChecks("/health", new HealthCheckOptions
{
    Predicate = p => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.UseHealthChecksUI(options => { options.UIPath = "/dashboard"; });


app.MapPost("/chat", async (ChatService service, EmbeddingService rag, [FromBody] MensagemDTO mensagem) =>
{
    return Results.Ok(await service.ProcessarChat(mensagem.Texto));
})
.WithName("Chat");

app.MapPost("/chat-markdown", async (ChatService service, [FromBody] MensagemDTO mensagem) =>
{
    var resultado = await service.ProcessarChat(mensagem.Texto);
    return Results.Ok(new MensagemDTO(resultado.ToMarkdown()));
})
.WithName("Chat-Markdown");


app.MapDefaultEndpoints();

app.Run();