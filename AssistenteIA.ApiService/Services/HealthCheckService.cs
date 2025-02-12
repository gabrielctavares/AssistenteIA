using AssistenteIA.ApiService.Models.DTOs;
using System.Text.Json;

namespace AssistenteIA.ApiService.Services;

public class HealthCheckService(HttpClient httpClient)
{

    public async Task<DadosDTO> VerificarServicos()
    {
        var dados = await ObterSaudeServicos();
        return new DadosDTO("Confira o estado dos serviços", dados);
    }
        
    private async Task<List<Dictionary<string, object>>> ObterSaudeServicos()
    {
        var result = new List<Dictionary<string, object>>();        
       
        foreach (var item in ObterServicos())
        {
            var dado = new Dictionary<string, object>
            {
                ["Nome"] = item.Nome,
                ["Endereço"] = item.URL
            };

            try
            {

                var response = await httpClient.GetAsync(item.URL);

                if (response.IsSuccessStatusCode)
                {
                    dado["Saúde"] = "Saudável";
                    dado["Mensagem"] = "O Serviço está saudável.";
                }
                else
                {
                    dado["Saúde"] = "Problema";
                    dado["Mensagem"] = $"Retorno: {response.StatusCode} - .";
                    
                }
            }
            catch (Exception ex)
            {
                dado["Saúde"] = "Problema";
                dado["Mensagem"] = $"Erro ao conectar ao serviço: {ex.Message}";
            }

            result.Add(dado);
        }
           
        
        return result;
    }
    private List<ServicoDTO> ObterServicos()
    {

        // Implementação inicial, melhorar depois para explorar os serviços da rede.

        return
        [
            new("Serviço 1", "https+http://servico1/health"),
            new("Serviço 2", "https+http://servico2/health"),
        ];
    }


}
