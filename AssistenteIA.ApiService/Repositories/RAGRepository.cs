using AssistenteIA.ApiService.Models;
using Dapper;
using Pgvector;

namespace AssistenteIA.ApiService.Repositories;

public class RAGRepository(IConfiguration configuration, ILogger<RAGRepository> logger): BaseRepository(configuration)
{

    const float MIN_SIMILARIDADE = 0.7f;
    public async Task<bool> InserirItem(RAGItem item, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await ObterConexao(cancellationToken);

            var sqlCheck = "SELECT COUNT(*) FROM treinamento_rag WHERE pergunta = @pergunta;";
            var sqlInsert = "INSERT INTO treinamento_rag (pergunta, resposta, embedding) VALUES (@pergunta, @resposta, @embedding)";
            
            var count = await connection.ExecuteScalarAsync<long>(sqlCheck, new { pergunta = item.Pergunta });
            if (count > 0)
                return false;

            var result = await connection.ExecuteAsync(sqlInsert, new { pergunta = item.Pergunta, resposta = item.Resposta, embedding = item.Embedding });
            return result > 0;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Erro ao executar consulta SQL.");
            return false;
        }
    }


    public async Task<List<RAGItem>> ObterProximos(Vector embedding, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await ObterConexao(cancellationToken);
            
            var sql = "SELECT id, pergunta, resposta, embedding FROM treinamento_rag WHERE 1 - (embedding <=> @embedding) >= @min_similaridade ORDER BY embedding <=> @embedding LIMIT 5;";
            return (await connection.QueryAsync<RAGItem>(sql, new { embedding, min_similaridade = MIN_SIMILARIDADE })).AsList();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Erro ao executar consulta SQL.");
            throw;
        }
    }
}
