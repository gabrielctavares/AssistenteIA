using Microsoft.Extensions.Logging;
using Npgsql;

namespace AssistenteIA.ApiService.Repositories;

public class QueryRepository(IConfiguration configuration, ILogger<QueryRepository> logger): BaseRepository(configuration)
{

    public async Task<List<Dictionary<string, object>>> ObterDados(string sql)
    {
        var resultado = new List<Dictionary<string, object>>();

        try
        {
            await using var connection = ObterConexao();
            await connection.OpenAsync();



            using var sqlCommand = new NpgsqlCommand(sql, connection);
            using var reader = await sqlCommand.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var linha = new Dictionary<string, object>();

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    linha[reader.GetName(i)] = reader.GetValue(i);
                }

                resultado.Add(linha);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Erro ao executar consulta SQL.");
            throw;
        }

        return resultado;
    }

}
