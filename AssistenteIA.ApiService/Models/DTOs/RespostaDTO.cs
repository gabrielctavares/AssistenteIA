using System.Text;

namespace AssistenteIA.ApiService.Models.DTOs;

public record RespostaDTO(string Mensagem, List<Dictionary<string, object>> Dados);
