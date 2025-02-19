using AssistenteIA.ApiService.Models.DTOs;
using AssistenteIA.ApiService.Models.Enums;
using System.Text;

namespace AssistenteIA.ApiService.Services;

public class ChatService(LLMService llmService, DatabaseService databaseService, HealthCheckService healthCheckService)
{
    public async Task<DadosDTO> ProcessarChat(string mensagem)
    {
        var classificacao = await ObterClassificacaoAtendimento(mensagem);

        return classificacao switch
        {
            ClassificacaoAtendimento.ConsultarOuAlterarDados => await databaseService.ConsultarDados(mensagem),
            ClassificacaoAtendimento.ConsultarFuncionamentoServicos => await healthCheckService.VerificarServicos(),
            _ => new DadosDTO("Não entendi sua pergunta.", []),
        };
    }


    private async Task<ClassificacaoAtendimento> ObterClassificacaoAtendimento(string mensagem)
    {

        var classificacaoAtendimento = await Task.FromResult("0"); //  await llmService.GerarResposta(mensagem, CriarPrompt());

        if (int.TryParse(classificacaoAtendimento, out int valor))
            return valor == 0 ? ClassificacaoAtendimento.ConsultarOuAlterarDados : ClassificacaoAtendimento.ConsultarFuncionamentoServicos;

        return ClassificacaoAtendimento.ConsultarOuAlterarDados;               
    }
    private string CriarPrompt()
    {
        StringBuilder promptBuilder = new();

        promptBuilder.AppendLine("Você é um assistente especializado em classificar perguntas.");
        promptBuilder.AppendLine("Sua tarefa é identificar a categoria da pergunta com base em seu conteúdo.");
        promptBuilder.AppendLine("Classifique a pergunta nas seguintes categorias:");
        promptBuilder.AppendLine("0 - Consultar ou alterar dados");
        promptBuilder.AppendLine("1 - Consultar o funcionamento de serviços");
        promptBuilder.AppendLine("### Resposta:");
        promptBuilder.AppendLine("- A resposta deve ser apenas o número da categoria do atendimento, 0 para Consulta/Alteração de dados e 1 para funcionamento de serviços.");

        return promptBuilder.ToString();
    }

}
