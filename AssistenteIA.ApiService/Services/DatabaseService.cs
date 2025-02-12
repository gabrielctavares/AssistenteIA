using AssistenteIA.ApiService.Models.DTOs;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;
using System.Text.Json;

namespace AssistenteIA.ApiService.Services;

public class DatabaseService(LLMService service, IConfiguration configuration, ILogger<DatabaseService> logger)
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection");

    public async Task<DadosDTO> ConsultarDados(string pergunta)
    {
        try
        {
            var prompt = await GerarPromptSQL();
            var resposta = await service.GerarResposta(pergunta, prompt);
            var respostaIA = TratarRespostaIA(resposta);

            
            if (respostaIA == null)
                throw new InvalidDataException("Resposta da IA não foi processada corretamente.");            

            if (!string.IsNullOrEmpty(respostaIA.SQL))
            {
                VerificaSQLSeguro(respostaIA.SQL);

                var dados = await ObterDados(respostaIA.SQL);
                return new DadosDTO(respostaIA.Mensagem, dados);
            }

            return new DadosDTO(respostaIA.Mensagem, []);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao consultar dados.");
            throw;
        }
    }

    private async Task<string> GerarPromptSQL()
    {
        StringBuilder promptBuilder = new();

        var schema = await ObterSchema();

        promptBuilder.AppendLine("Você é um assistente especializado em SQL. Converta a pergunta em uma consulta SQL segura.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("### Utilizando o schema do banco de dados abaixo:");
        promptBuilder.AppendLine($"{schema}");
        promptBuilder.AppendLine("Em caso de modificações, gere um SELECT para mostrar as alterações.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("### Regras:");
        promptBuilder.AppendLine("- Não altere a estrutura das tabelas.");
        promptBuilder.AppendLine("- Não crie novas tabelas.");
        promptBuilder.AppendLine("- Não use DELETE.");
        promptBuilder.AppendLine("- Evite UPDATE sem WHERE.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("### Resposta:");
        promptBuilder.AppendLine("- A resposta deve ser um JSON com os campos \"Mensagem\" e \"SQL\".");
        promptBuilder.AppendLine("- \"Mensagem\": descrição amigável sobre os dados, sem citar SQL.");
        promptBuilder.AppendLine("- \"SQL\": a consulta SQL gerada, garantindo nomes de coluna amigáveis usando aliases.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Exemplo: {\"Mensagem\": \"Aqui está os dados solicitados\", \"SQL\": \"SELECT id as Código, nome as Nome FROM Tabela;\"}");


        return promptBuilder.ToString();
    }

    private async Task<string> ObterSchema()
    {
        var schemaBuilder = new StringBuilder();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            DataTable tables = connection.GetSchema("Tables");

            foreach (DataRow tableRow in tables.Rows)
            {
                string tableName = tableRow["TABLE_NAME"].ToString();
                schemaBuilder.AppendLine($"Tabela: {tableName}");

                DataTable columns = connection.GetSchema("Columns", [tableName]);
                foreach (DataRow col in columns.Rows)
                {
                    string columnName = col["COLUMN_NAME"].ToString();
                    string dataType = col["DATA_TYPE"].ToString();
                    schemaBuilder.AppendLine($"  - {columnName} ({dataType})");
                }

                DataTable primaryKeys = connection.GetSchema("IndexColumns", [tableName]);

                if (primaryKeys.Rows.Count > 0)
                {
                    schemaBuilder.AppendLine("  >> Primary Key(s):");
                    foreach (DataRow pkRow in primaryKeys.Rows)
                    {
                        string pkColumn = pkRow["COLUMN_NAME"].ToString();
                        schemaBuilder.AppendLine($"    - {pkColumn}");
                    }
                }
            }

            return schemaBuilder.ToString();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Erro ao obter esquema do banco de dados.");
            throw;
        }
    }


    private ConsultaSQLDTO TratarRespostaIA(string resposta)
    {
        try
        {
            int inicio = resposta.IndexOf('{');
            int fim = resposta.LastIndexOf('}');

            if (inicio == -1 || fim == -1)
                return null;

            var jsonResposta = resposta.Substring(inicio, fim - inicio + 1);
            return JsonSerializer.Deserialize<ConsultaSQLDTO>(jsonResposta);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao processar resposta da IA.");
            return null;
        }
    }

    private void VerificaSQLSeguro(string sql)
    {
        var sqlUpper = sql.ToUpperInvariant();
        var comandosProibidos = new[] { "DELETE", "DROP", "TRUNCATE", "ALTER" };

        if (comandosProibidos.Any(x => sqlUpper.Contains(x)))
            throw new InvalidOperationException("o SQL gerado não é permitido");

        bool isUpdate = sqlUpper.Contains("UPDATE");

        if (isUpdate)
        {
            // LOGA o Update para reverter em caso de problemas
            logger.LogInformation("SQL UPDATE executado: {Sql}", sql);
        }
            

    }
    private async Task<List<Dictionary<string, object>>> ObterDados(string sql)
    {
        var resultado = new List<Dictionary<string, object>>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var sqlCommand = new SqlCommand(sql, connection);
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
