using System.Runtime.CompilerServices;
using System.Text;

namespace AssistenteIA.ApiService.Models.DTOs;

public record DadosDTO(string Mensagem, List<Dictionary<string, object>> Dados);

public static class DadosExtension
{
    public static string ToMarkdown(this DadosDTO dados)
    {
        if (dados.Dados == null || dados.Dados.Count == 0)
            return dados.Mensagem;

        var markdownBuilder = new StringBuilder();
        markdownBuilder.AppendLine($"*{dados.Mensagem}*");
        markdownBuilder.AppendLine();

        var colunas = dados.Dados.First().Keys.ToList();
        markdownBuilder.AppendLine("| " + string.Join(" | ", colunas) + " |");
        markdownBuilder.AppendLine("|" + string.Join("|", colunas.Select(c => new string('-', c.Length + 2))) + "|");

        foreach (var linha in dados.Dados)
            markdownBuilder.AppendLine(" | " + string.Join(" | ", linha.Values) + " | ");

        return markdownBuilder.ToString();
    }
}