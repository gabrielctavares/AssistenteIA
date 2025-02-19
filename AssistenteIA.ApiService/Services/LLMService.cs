
using Microsoft.Extensions.AI;
using System.Text;
namespace AssistenteIA.ApiService.Services;

public class LLMService(Microsoft.Extensions.AI.IChatClient client, ILogger<LLMService> logger)
{
    public async Task<string> GerarResposta(string userQuery, string prompt = null)
    {
        try
        {
            if (string.IsNullOrEmpty(prompt))
                prompt = CriarPromptPadrao();

            var mensagemSistema = new ChatMessage(ChatRole.System, prompt);
            var mensagemCliente = new ChatMessage(ChatRole.User, userQuery);

            var answer = await client.CompleteAsync([mensagemSistema, mensagemCliente]);

            return answer.Choices.FirstOrDefault()?.Text!;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Erro ao processar resposta");
            throw;
        }

    }

    private static string CriarPromptPadrao()
    {
        StringBuilder promptBuilder = new();

        promptBuilder.AppendLine("Você é um assistente inteligente que responde de forma clara e objetiva em Português-BR.");
        promptBuilder.AppendLine("Seu objetivo é ajudar da melhor forma possível, adaptando o tom da conversa conforme necessário.");
        promptBuilder.AppendLine("Use exemplos e explicações simples quando relevante, sem excesso de detalhes.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("### Regras:");
        promptBuilder.AppendLine("- Se a pergunta for direta, responda de forma breve e objetiva.");
        promptBuilder.AppendLine("- Se o usuário pedir detalhes, forneça explicações mais estruturadas.");
        promptBuilder.AppendLine("- Evite termos técnicos, a menos que sejam essenciais para a resposta.");
        promptBuilder.AppendLine("- Se o usuário fizer perguntas relacionadas a SQL, forneça respostas práticas sem excesso de teoria.");
        promptBuilder.AppendLine("- Para consultas: Retorne os dados conforme solicitado, otimizando a busca.");
        promptBuilder.AppendLine("- Para correções: Atualize apenas os registros necessários, garantindo consistência.");
        promptBuilder.AppendLine("- Evite respostas excessivamente técnicas, a menos que solicitado.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("### Restrições:");
        promptBuilder.AppendLine("- Não altere a estrutura das tabelas.");
        promptBuilder.AppendLine("- Não crie novas tabelas.");
        promptBuilder.AppendLine("- Priorize consultas eficientes.");
        promptBuilder.AppendLine("### Estilo de Resposta:");
        promptBuilder.AppendLine("- Seja educado e amigável.");
        promptBuilder.AppendLine("- Priorize a clareza e a simplicidade.");
        promptBuilder.AppendLine("- Evite repetições desnecessárias.");

        return promptBuilder.ToString();
    }
    
}
