using AssistenteIA.ApiService.Models;
using AssistenteIA.ApiService.Services;
using AssistenteIA.ApiService.Repositories;

using System.ClientModel;
using OpenAI;
using Azure.AI.Inference;
using Microsoft.Extensions.AI;


namespace AssistenteIA.ApiService.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection ConfigureRepositories(this IServiceCollection services)
    {
        services.AddScoped<QueryRepository>();
        services.AddScoped<MetadataRepository>();
        services.AddScoped<RAGRepository>();
        return services;
    }
    public static IServiceCollection ConfigureServices(this IServiceCollection services)
    {

        services.AddHttpClient();

        services.AddScoped<RAGService>();
        services.AddScoped<LLMService>();

        services.AddScoped<DatabaseService>();
        services.AddScoped<HealthCheckService>();

        services.AddScoped<ChatService>();        
        return services;
    }
}