using AssistenteIA.ApiService.Models.DTOs;
using AssistenteIA.ApiService.Repositories;
using System.Text;
using System.Text.Json;

namespace AssistenteIA.ApiService.Services;

public class DatabaseService(LLMService service, MetadataRepository metadataRepository, QueryRepository queryRepository,  ILogger<DatabaseService> logger)
{

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

                var dados = await queryRepository.ObterDados(respostaIA.SQL);


                //TEMPORÁRIO: Adiciona o SQL gerado para debug
                var mensagemDebug = $"<br> ***Debug:*** \n ```sql\n{respostaIA.SQL}\n```";

                return new DadosDTO(respostaIA.Mensagem + mensagemDebug, dados);
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

        var schema = await FormatarSchema();

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


    private async Task<string> FormatarSchema()
    {
        StringBuilder schemaBuilder = new();
        var metadata = await metadataRepository.ObterMetadata();

        foreach (var tabela in metadata)
        {
            schemaBuilder.AppendLine($"Tabela: {tabela.Tabela}");

            foreach (var campo in tabela.Campos)
            {
                if (!string.IsNullOrWhiteSpace(campo.Descricao))
                    schemaBuilder.AppendLine($"  - {campo.Nome} ({campo.Tipo}) - {campo.Descricao}");
                else
                    schemaBuilder.AppendLine($"  - {campo.Nome} ({campo.Tipo})");
            }


            if (tabela.PrimaryKeys.Count != 0)
            {
                schemaBuilder.AppendLine("  >> Primary Key(s):");
                schemaBuilder.AppendLine(string.Join(Environment.NewLine, tabela.PrimaryKeys.Select(pk => $"    - {pk}")));
            }

            if (tabela.ForeignKeys.Count != 0)
            {
                schemaBuilder.AppendLine("  >> Foreign Key(s):");
                schemaBuilder.AppendLine(string.Join(Environment.NewLine, tabela.ForeignKeys.Select(fk => $"    - {fk.Coluna} -> {fk.TabelaReferenciada}({fk.ColunaReferenciada})")));
            }

            if (tabela.UniqueKeys.Count != 0)
            {
                schemaBuilder.AppendLine("  >> Unique Key(s):");
                schemaBuilder.AppendLine(string.Join(Environment.NewLine, tabela.UniqueKeys.Select(unique => $"    - {unique}")));
            }


            schemaBuilder.AppendLine();
        }

        return schemaBuilder.ToString();
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
    
}
