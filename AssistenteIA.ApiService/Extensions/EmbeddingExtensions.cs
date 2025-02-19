using AssistenteIA.ApiService.Models;
using Azure.AI.Inference;
using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

namespace AssistenteIA.ApiService.Extensions;

using IEmbeddingClient = IEmbeddingGenerator<string, Embedding<float>>;
public static class EmbeddingExtensions
{

    public static IServiceCollection ConfigureEmbeddingClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IEmbeddingClient>(sp => CriarEmbeddingGenerator(configuration));

        return services;

    }

    private static IEmbeddingClient CriarEmbeddingGenerator(IConfiguration configuration)
    {
        var _configuration = configuration.GetSection("AIConfig").Get<AIConfig>()
            ?? throw new InvalidOperationException("A seção 'AIConfig' não foi encontrada ou está mal formatada.");

        if (true || _configuration.Provider.Equals("ollama", StringComparison.OrdinalIgnoreCase))
        {
            if (_configuration.Ollama is null)
                throw new InvalidOperationException("A seção 'Ollama' não foi encontrada ou está mal formatada.");

            return CriarEmbeddingOllama(_configuration.Ollama);

        }
        else if (_configuration.Provider.Equals("openAI", StringComparison.OrdinalIgnoreCase))
        {
            if (_configuration.OpenAI is null)
                throw new InvalidOperationException("A seção 'OpenAI' não foi encontrada ou está mal formatada.");

            return CriarEmbeddingOpenAI(_configuration.OpenAI);
        }
        else
        {
            if (_configuration.Azure is null)
                throw new InvalidOperationException("A seção 'Azure' não foi encontrada ou está mal formatada.");

            return CriarEmbeddingAzure(_configuration.Azure);
        }
    }

    private static OllamaEmbeddingGenerator CriarEmbeddingOllama(OllamaConfig ollamaConfig)
    {
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri(ollamaConfig.Uri),
            Timeout = TimeSpan.FromMinutes(5) // Limite alto por demora na resposta

        };

        return new OllamaEmbeddingGenerator(new Uri(ollamaConfig.Uri), ollamaConfig.EmbeddingModel, httpClient);
    }

    private static OpenAIEmbeddingGenerator CriarEmbeddingOpenAI(OpenAIConfig openAIConfig)
    {
        ApiKeyCredential key = new(openAIConfig.ApiKey);

        OpenAIClientOptions options = new();

        // Se não preencher ele usa o da OpenAI, preenchido ele pode usar o DeepSeek, OpenRouter e etc.
        if (!string.IsNullOrEmpty(openAIConfig.Uri))
            options.Endpoint = new Uri(openAIConfig.Uri);

        var client = new OpenAIClient(key, options);
        return new OpenAIEmbeddingGenerator(client, openAIConfig.EmbeddingModel);

    }
    private static AzureAIInferenceEmbeddingGenerator CriarEmbeddingAzure(AzureConfig azureConfig)
    {
        EmbeddingsClient azureClient = new(new Uri(azureConfig.Uri), new Azure.AzureKeyCredential(azureConfig.ApiKey));
        return new AzureAIInferenceEmbeddingGenerator(azureClient, azureConfig.DeploymentName);
    }

}
