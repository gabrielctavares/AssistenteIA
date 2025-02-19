using System.Text.Json;

namespace AssistenteIA.Web;

public class AssistenteIAApiClient(HttpClient httpClient)
{
    public async Task<DadosDTO> PostMessage(string chat, CancellationToken cancellationToken = default)
    {
        using var hhtp = new HttpClient()
        {
            BaseAddress = new("https://localhost:7396"),
            Timeout = Timeout.InfiniteTimeSpan
        };

        var response = await hhtp.PostAsJsonAsync("/chat", new ChatMessage(chat), cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DadosDTO>(cancellationToken))!;
    }

    public async Task<ChatMessage> PostMessageMarkdown(string chat, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/chat-markdown", new ChatMessage(chat), cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ChatMessage>(cancellationToken))!;
    }
}
public record ChatMessage(string Texto);
public record DadosDTO(string Mensagem, List<Dictionary<string, object>> Dados);

