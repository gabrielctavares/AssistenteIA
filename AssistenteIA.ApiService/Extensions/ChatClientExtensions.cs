using AssistenteIA.ApiService.Models;
using Azure.AI.Inference;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

namespace AssistenteIA.ApiService.Extensions;

public static class ChatClientExtensions
{
    public static IServiceCollection ConfigureChatClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IChatClient>(sp => CriarChatClient(configuration));      
        return services;
    }


    private static IChatClient CriarChatClient(IConfiguration configuration)
    {
        var _configuration = configuration.GetSection("AIConfig").Get<AIConfig>()
            ?? throw new InvalidOperationException("A seção 'AIConfig' não foi encontrada ou está mal formatada.");

        if (_configuration.Provider.Equals("ollama", StringComparison.OrdinalIgnoreCase))
        {
            if (_configuration.Ollama is null)
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
    private static OllamaChatClient CriarChatOllama(OllamaConfig ollamaConfig)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(ollamaConfig.Uri),
            Timeout = TimeSpan.FromMinutes(5) // Limite alto por demora na resposta

        };

        return new OllamaChatClient(new Uri(ollamaConfig.Uri), ollamaConfig.Model, httpClient);
    }

    private static OpenAIChatClient CriarChatOpenAI(OpenAIConfig openAIConfig)
    {
        ApiKeyCredential key = new(openAIConfig.ApiKey);

        OpenAIClientOptions options = new();

        // Se não preencher ele usa o da OpenAI, preenchido ele pode usar o DeepSeek, OpenRouter e etc.
        if (!string.IsNullOrEmpty(openAIConfig.Uri))
            options.Endpoint = new Uri(openAIConfig.Uri);

        var client = new OpenAIClient(key, options);

        return new OpenAIChatClient(client, openAIConfig.Model);

    }
    private static AzureAIInferenceChatClient CriarChatAzure(AzureConfig azureConfig)
    {
        ChatCompletionsClient azureClient = new(new Uri(azureConfig.Uri), new Azure.AzureKeyCredential(azureConfig.ApiKey));

        return new AzureAIInferenceChatClient(azureClient, azureConfig.DeploymentName);
    }

}
