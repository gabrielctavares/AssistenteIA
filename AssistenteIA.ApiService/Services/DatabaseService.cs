using AssistenteIA.ApiService.Models.DTOs;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Npgsql;
using System.Data;
using System.Text;
using System.Text.Json;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

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
                return new DadosDTO(respostaIA.Mensagem + $"<br> ***Debug:*** \n ```sql\n{respostaIA.SQL}\n```", dados);
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
        promptBuilder.AppendLine("- Somente SQL Independente de parâmetros.");
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
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var tableNames = new List<string>();
            string tableQuery = @"SELECT table_name  
                                  FROM information_schema.tables 
                                  WHERE table_schema = 'public' AND table_type = 'BASE TABLE';";

            await using var tableCmd = new NpgsqlCommand(tableQuery, connection);
            await using var reader = await tableCmd.ExecuteReaderAsync();            
            while (await reader.ReadAsync())
                tableNames.Add(reader.GetString(0));
            

            foreach (var tableName in tableNames)
            {
                schemaBuilder.Append(FormatarTabela(tableName, connection));
            }

            return schemaBuilder.ToString();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Erro ao obter esquema do banco de dados.");
            throw;
        }
    }

    private async Task<string> FormatarTabela(string tabela, NpgsqlConnection connection)
    {
        var tableBuilder = new StringBuilder();
        tableBuilder.AppendLine($"Tabela: {tabela}");        
        tableBuilder.AppendLine(await FormatarColunas(tabela, connection));
        tableBuilder.AppendLine(await FormatarPrimaryKeys(tabela, connection));
        tableBuilder.AppendLine(await FormatarForeignKeys(tabela, connection));
        return tableBuilder.ToString();
    }

    private async Task<string> FormatarColunas(string tabela, NpgsqlConnection connection)
    {
        var columnBuilder = new StringBuilder();

        string columnQuery = @"
                SELECT 
                    c.column_name, 
                    c.data_type, 
                    pgd.description AS column_comment
                FROM information_schema.columns c
                LEFT JOIN pg_catalog.pg_statio_all_tables st 
                    ON c.table_schema = st.schemaname AND c.table_name = st.relname
                LEFT JOIN pg_catalog.pg_description pgd 
                    ON pgd.objoid = st.relid AND pgd.objsubid = c.ordinal_position
                WHERE c.table_schema = 'public' AND c.table_name = @tableName;
            ";

        await using var colCmd = new NpgsqlCommand(columnQuery, connection);
        colCmd.Parameters.AddWithValue("tableName", tabela);
        await using var colReader = await colCmd.ExecuteReaderAsync();

        while (await colReader.ReadAsync())
        {
            string columnName = colReader.GetString(0);
            string dataType = colReader.GetString(1);
            string columnComment = colReader.IsDBNull(2) ? "" : colReader.GetString(2);

            if (!string.IsNullOrWhiteSpace(columnComment))
                columnBuilder.AppendLine($"  - {columnName} ({dataType}) - {columnComment}");
            else
                columnBuilder.AppendLine($"  - {columnName} ({dataType})");
        }

        return columnBuilder.ToString();
    }

    private async Task<string> FormatarPrimaryKeys(string tabela, NpgsqlConnection connection)
    {
        var pkBuilder = new StringBuilder();

        string pkQuery = @"
                SELECT kcu.column_name
                FROM information_schema.table_constraints tc
                JOIN information_schema.key_column_usage kcu
                  ON tc.constraint_name = kcu.constraint_name
                 AND tc.table_schema = kcu.table_schema
                WHERE tc.constraint_type = 'PRIMARY KEY'
                  AND tc.table_name = @tableName;
            ";

        await using var pkCmd = new NpgsqlCommand(pkQuery, connection);
        pkCmd.Parameters.AddWithValue("tableName", tabela);
        await using var pkReader = await pkCmd.ExecuteReaderAsync();
        if (pkReader.HasRows)
        {
            pkBuilder.AppendLine("  >> Primary Key(s):");
            while (await pkReader.ReadAsync())
            {
                string pkColumn = pkReader.GetString(0);
                pkBuilder.AppendLine($"    - {pkColumn}");
            }
        }

        return pkBuilder.ToString();
    }

    private async Task<string> FormatarForeignKeys(string tabela, NpgsqlConnection connection)
    {
        var pkBuilder = new StringBuilder();

        string fkQuery = @"
                SELECT 
                    kcu.column_name, 
                    ccu.table_name AS foreign_table_name, 
                    ccu.column_name AS foreign_column_name
                FROM information_schema.table_constraints tc 
                JOIN information_schema.key_column_usage kcu
                  ON tc.constraint_name = kcu.constraint_name
                 AND tc.table_schema = kcu.table_schema
                JOIN information_schema.constraint_column_usage ccu
                  ON ccu.constraint_name = tc.constraint_name
                 AND ccu.table_schema = tc.table_schema
                WHERE tc.constraint_type = 'FOREIGN KEY'
                  AND tc.table_name = @tableName;
            ";

        await using var fkCmd = new NpgsqlCommand(fkQuery, connection);
        fkCmd.Parameters.AddWithValue("tableName", tabela);

        await using var fkReader = await fkCmd.ExecuteReaderAsync();
        if (fkReader.HasRows)
        {
            pkBuilder.AppendLine("  >> Foreign Key(s):");
            while (await fkReader.ReadAsync())
            {
                string fkColumn = fkReader.GetString(0);
                string foreignTable = fkReader.GetString(1);
                string foreignColumn = fkReader.GetString(2);
                pkBuilder.AppendLine($"    - {fkColumn} -> {foreignTable}({foreignColumn})");
            }
        }

        return pkBuilder.ToString();
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
            using var connection = new NpgsqlConnection(_connectionString);
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
