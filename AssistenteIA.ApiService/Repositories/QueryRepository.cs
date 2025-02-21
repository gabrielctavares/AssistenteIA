using Dapper;

namespace AssistenteIA.ApiService.Repositories;

public class QueryRepository(IConfiguration configuration, ILogger<QueryRepository> logger): BaseRepository(configuration)
{

    public async Task<List<Dictionary<string, object>>> ObterDados(string sql, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await ObterConexao(cancellationToken);

            var resultado = (await connection.QueryAsync(sql))
                .Select(row => (IDictionary<string, object>)row)
                .Select(dict => dict.ToDictionary(k => k.Key, v => v.Value))
                .ToList();

            return resultado;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Erro ao executar consulta SQL.");
            throw;
        }
    }


}
