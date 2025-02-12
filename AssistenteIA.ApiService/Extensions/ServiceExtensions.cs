using System.ClientModel;
using Microsoft.Extensions.AI;
using AssistenteIA.ApiService.Models;
using AssistenteIA.ApiService.Services;

using OpenAI;
using OllamaSharp;
using Azure.AI.OpenAI;


namespace AssistenteIA.ApiService.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IChatClient>(sp =>
        {
            return CriarChatClient(configuration);
        });

        services.AddScoped<LLMService>();
        services.AddScoped<DatabaseService>();

        services.AddHttpClient();
        services.AddScoped<HealthCheckService>();

        services.AddScoped<ChatService>();


        return services;
    }

    private static IChatClient CriarChatClient(IConfiguration configuration)
    {
        var _configuration = configuration.GetSection("AIConfig").Get<AIConfig>()
            ?? throw new InvalidOperationException("A seção 'AIConfig' não foi encontrada ou está mal formatada.");

        if (_configuration.Provider.Equals("ollama", StringComparison.OrdinalIgnoreCase))
        {
            if(_configuration.Ollama is null)
                throw new InvalidOperationException("A seção 'Ollama' não foi encontrada ou está mal formatada.");

            return CriarChatOllama(_configuration.Ollama);
            
        }
        else if (_configuration.Provider.Equals("openAI", StringComparison.OrdinalIgnoreCase))
        {
            if (_configuration.OpenAI is null)
                throw new InvalidOperationException("A seção 'OpenAI' não foi encontrada ou está mal formatada.");

            return CriarChatOpenAI(_configuration.OpenAI);
        }
        else
        {
            if (_configuration.Azure is null)
                throw new InvalidOperationException("A seção 'Azure' não foi encontrada ou está mal formatada.");

            return CriarChatAzure(_configuration.Azure);            
        }
    }

    private static OllamaApiClient CriarChatOllama(OllamaConfig ollamaConfig) {
        return new OllamaApiClient(
            new HttpClient
            {
                BaseAddress = new Uri(ollamaConfig.Uri),
                Timeout = TimeSpan.FromMinutes(5) // Limite alto por demora na resposta
            }, 
            ollamaConfig.Model
        );
    }

    private static IChatClient CriarChatOpenAI(OpenAIConfig openAIConfig) {
        ApiKeyCredential key = new(openAIConfig.ApiKey);

        OpenAIClientOptions options = new();

        // Se não preencher ele usa o da OpenAI, preenchido ele pode usar o DeepSeek, OpenRouter e etc.
        if (!string.IsNullOrEmpty(openAIConfig.Uri)) 
            options.Endpoint = new Uri(openAIConfig.Uri);

        return new OpenAIClient(key, options).AsChatClient(openAIConfig.Model);

    }
    private static IChatClient CriarChatAzure(AzureConfig azureConfig) {
        AzureOpenAIClient azureClient = new(new Uri(azureConfig.Uri), new ApiKeyCredential(azureConfig.ApiKey));

        return azureClient.GetChatClient(azureConfig.DeploymentName).AsChatClient();
    }

}