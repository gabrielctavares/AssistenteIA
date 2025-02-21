using System.Text.Json;

namespace AssistenteIA.Web;

public class AssistenteIAApiClient(HttpClient httpClient)
{
    public async Task<DadosDTO> PostMessage(string chat, CancellationToken cancellationToken = default)
    {
        using var http = new HttpClient()
        {
            BaseAddress = new("https://localhost:7396"),
            Timeout = Timeout.InfiniteTimeSpan
        };

        var response = await http.PostAsJsonAsync("/chat", new ChatMessage(chat), cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DadosDTO>(cancellationToken))!;
    }

    public async Task<HttpResponseMessage> PostTreinarRag(IList<RAGItem> itens, CancellationToken cancellationToken = default)
    {
        using var http = new HttpClient()
        {
            BaseAddress = new("https://localhost:7396"),
            Timeout = Timeout.InfiniteTimeSpan
        };

        var response = await http.PostAsJsonAsync("/treinar-rag", itens, cancellationToken);
        response.EnsureSuccessStatusCode();
        return response;
    }
}
public record ChatMessage(string Texto);
public record DadosDTO(string Mensagem, List<Dictionary<string, object>> Dados);
public class RAGItem
{
    public string Pergunta { get; set; } = default!;
    public string Resposta { get; set; } = default!;
}

