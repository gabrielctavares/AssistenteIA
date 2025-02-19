using System.Text.Json;
using System.Collections.ObjectModel;
using Microsoft.Extensions.AI;
using System.Text;

namespace AssistenteIA.ApiService.Services;

using IEmbeddingClient = IEmbeddingGenerator<string, Embedding<float>>;



public class EmbeddingService(IEmbeddingClient embeddingGenerator, ILogger<EmbeddingService> logger)
{
    const float MIN_SIMILARIDADE = 0.7f;

    public async Task<string> GerarEmbedding(string texto)
    {
        try
        {
            var trainingDataPath = "G:\\Projetos\\AssistenteIA\\AssistenteIA.ApiService\\Data\\RAGBaseConhecimento.json";

            var trainingData = await LoadTrainingDataAsync(trainingDataPath);
            if (trainingData == null || trainingData.Count == 0)
            {
                logger.LogWarning("Dados de treinamento vazios ou nulos.");
                return "Não há dados para treinamento.";
            }

            var trainingEmbeddings = await GenerateEmbeddingsAsync(trainingData, embeddingGenerator);
            var queryEmbeddingResponse = await embeddingGenerator.GenerateEmbeddingAsync(texto);
            float[] queryEmbedding = queryEmbeddingResponse.Vector.ToArray();

            var matches = trainingEmbeddings
                .Select(item => new
                {
                    item.qa,
                    Similaridade = CosineSimilarity(queryEmbedding, item.embedding)
                })
                .Where(x => x.Similaridade >= MIN_SIMILARIDADE) 
                .OrderByDescending(x => x.Similaridade)
                .ToList();

            if (matches.Count == 0)
            {
                logger.LogWarning("Nenhum match encontrado com similaridade aceitável.");
                return texto;
            }

            var topMatch = matches.First();

            var prompt = new StringBuilder();
            prompt.AppendLine($"Embedding encontrado:");
            prompt.AppendLine("Pergunta: " + topMatch.qa.Pergunta);
            prompt.AppendLine("Resposta: " + topMatch.qa.SQL);
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

    private static async Task<List<(RAGMensagem qa, float[] embedding)>> GenerateEmbeddingsAsync(
        List<RAGMensagem> qaPairs, IEmbeddingClient client)
    {
        var tasks = qaPairs.Select(async qa =>
        {
            var embeddingResponse = await client.GenerateEmbeddingAsync(qa.Pergunta);
            return (qa, embedding: embeddingResponse.Vector.ToArray());
        });

        return [.. await Task.WhenAll(tasks)];
    }

    private static async Task<List<RAGMensagem>> LoadTrainingDataAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Arquivo de dados não encontrado.", filePath);

        string json = await File.ReadAllTextAsync(filePath);
        var data = JsonSerializer.Deserialize<List<RAGMensagem>>(json);
        return data ?? [];
    }

    // Calcula a similaridade do cosseno entre dois vetores com validação
    private static float CosineSimilarity(float[] v1, float[] v2)
    {
        if (v1.Length != v2.Length)
            throw new ArgumentException("Vetores de tamanho diferentes.");

        float dot = 0, mag1 = 0, mag2 = 0;
        for (int i = 0; i < v1.Length; i++)
        {
            dot += v1[i] * v2[i];
            mag1 += v1[i] * v1[i];
            mag2 += v2[i] * v2[i];
        }

        double magnitude = Math.Sqrt(mag1) * Math.Sqrt(mag2);
        if (magnitude == 0) return 0; // Evita divisão por zero

        return dot / (float)magnitude;
    }


    // Classe para mapear os dados JSON
    public class RAGMensagem
    {
        public string Pergunta { get; set; }
        public string SQL { get; set; }
    }
}

