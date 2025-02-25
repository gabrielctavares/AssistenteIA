using Microsoft.Extensions.AI;
namespace AssistenteIA.ApiService.Models;

public static class HistoricoChat
{
    const int MAX_MESSAGES = 10;

    private static readonly LinkedList<ChatMessage> _mensagens = new();

    public static void AddMensagemSistema(string message) => AddMensagem(ChatRole.System, message);
    public static void AddMensagemUsuario(string message) => AddMensagem(ChatRole.User, message);
    public static void AddMensagemAssistente(string message) => AddMensagem(ChatRole.Assistant, message);

    private static void AddMensagem(ChatRole role, string message)
    {
        if (_mensagens.Count >= MAX_MESSAGES)
        {
            var node = _mensagens.First;
            while (node != null && node.Value.Role == ChatRole.System)
            {
                node = node.Next;
            }

            if (node != null) 
            {
                _mensagens.Remove(node);
            }
        }

        _mensagens.AddLast(new ChatMessage(role, message)); 
    }

    public static IList<ChatMessage> ObterHistorico(int maxMessages = MAX_MESSAGES) => [.. _mensagens.TakeLast(maxMessages)];
    public static bool EstaVazio() => _mensagens.Count == 0;
}
