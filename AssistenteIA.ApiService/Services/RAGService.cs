using Microsoft.ML;
using Microsoft.ML.Data;
using System.Text.Json;

namespace AssistenteIA.ApiService.Services;

public static class RAGService
{
    public static void TestarRAG(string texto)
    {
        var mlContext = new MLContext();

        var data = LoadDataFromJson("G:\\Projetos\\AssistenteIA\\AssistenteIA.ApiService\\Data\\RAGBaseConhecimento.json");

        var trainingData = mlContext.Data.LoadFromEnumerable(data);

        var pipeline = mlContext.Transforms.Text.FeaturizeText("Features", nameof(QuestionSqlPair.Pergunta))
            .Append(mlContext.Transforms.Conversion.MapValueToKey("Label", nameof(QuestionSqlPair.SQL)))
            .Append(mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy())
            .Append(mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

        var model = pipeline.Fit(trainingData);

        var predictionEngine = mlContext.Model.CreatePredictionEngine<QuestionSqlPair, SqlPrediction>(model);

        var bestMatchSql = FindClosestQuery(texto, [.. data.Select(d => d.Pergunta)], [.. data.Select(d => d.SQL)]);
        var prediction = predictionEngine.Predict(new QuestionSqlPair { Pergunta = texto, SQL = bestMatchSql });

        Console.WriteLine($"Pergunta: {texto}");
        Console.WriteLine($"SQL Previsto: {prediction.SqlQuery}");
        Console.WriteLine($"SQL Mais Próximo: {bestMatchSql}");
    }

    public class QuestionSqlPair
    {
        [LoadColumn(0)]
        public string Pergunta { get; set; }

        [LoadColumn(1)]
        public string SQL { get; set; }
    }

    public class SqlPrediction
    {
        [ColumnName("PredictedLabel")]
        public string SqlQuery { get; set; }
    }

    static string FindClosestQuery(string userQuestion, List<string> questions, List<string> queries)
    {
        int bestIndex = 0;
        double bestScore = 0;

        for (int i = 0; i < questions.Count; i++)
        {
            double similarity = ComputeSimilarity(userQuestion, questions[i]);
            if (similarity > bestScore)
            {
                bestScore = similarity;
                bestIndex = i;
            }
        }

        return queries[bestIndex];
    }

    static double ComputeSimilarity(string a, string b)
    {
        var wordsA = new HashSet<string>(a.ToLower().Split(' '));
        var wordsB = new HashSet<string>(b.ToLower().Split(' '));

        var intersection = wordsA.Intersect(wordsB).Count();
        var union = wordsA.Union(wordsB).Count();

        return (double)intersection / union;
    }

    static List<QuestionSqlPair> LoadDataFromJson(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"⚠️ Arquivo {filePath} não encontrado!");
            return [];
        }

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<List<QuestionSqlPair>>(json);
    }

}