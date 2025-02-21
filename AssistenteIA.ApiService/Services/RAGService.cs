using System.Text.Json;
using System.Collections.ObjectModel;
using Microsoft.Extensions.AI;
using System.Text;
using AssistenteIA.ApiService.Repositories;
using AssistenteIA.ApiService.Models.DTOs;
using AssistenteIA.ApiService.Models;

namespace AssistenteIA.ApiService.Services;

using IEmbeddingClient = IEmbeddingGenerator<string, Embedding<float>>;

public class RAGService(IEmbeddingClient embeddingGenerator, RAGRepository repository,  ILogger<RAGService> logger)
{
    public async Task<string> GerarEmbedding(string texto, CancellationToken cancellationToken = default)
    {
        try
        {
            var queryEmbeddingResponse = await embeddingGenerator.GenerateEmbeddingAsync(texto, cancellationToken: cancellationToken);
            float[] queryEmbedding = queryEmbeddingResponse.Vector.ToArray();
            var matches = await repository.ObterProximos(new Pgvector.Vector(queryEmbedding), cancellationToken);

            if (matches.Count == 0)
            {
                logger.LogWarning("Nenhum match encontrado com similaridade aceitável.");
                return texto;
            }

            var topMatch = matches.First();
            var prompt = new StringBuilder();
            prompt.AppendLine($"Embedding encontrado:");
            prompt.AppendLine("Pergunta: " + topMatch.Pergunta);
            prompt.AppendLine("Resposta: " + topMatch.Resposta);
            prompt.AppendLine();
            prompt.AppendLine("Pergunta do usuário: " + texto);
            return prompt.ToString();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Erro ao executar TestarRAG");
            throw;
        }
    }

    public async Task TreinarRAG(List<RAGItemDTO> itens, CancellationToken cancellationToken = default)
    {
        try
        {
            var ragItems = await CalcularEmbeddingsAsync(itens, cancellationToken: cancellationToken);
            var tasks = ragItems.Select(x => repository.InserirItem(x, cancellationToken));
            await Task.WhenAll(tasks);  
        }
        catch (Exception e)
        {
            logger.LogError(e, "Erro ao treinar RAG.");
            throw;
        }

    }
    private async Task<List<RAGItem>> CalcularEmbeddingsAsync(List<RAGItemDTO> itens, CancellationToken cancellationToken = default)
    {
        var tasks = itens.Select(item =>
            embeddingGenerator.GenerateEmbeddingAsync(item.Pergunta, cancellationToken: cancellationToken)
            .ContinueWith(task => new RAGItem
            {
                Pergunta = item.Pergunta,
                Resposta = item.Resposta,
                Embedding = new Pgvector.Vector(task.Result.Vector.ToArray())
            }, cancellationToken));

        return [.. await Task.WhenAll(tasks)];
    }
}

