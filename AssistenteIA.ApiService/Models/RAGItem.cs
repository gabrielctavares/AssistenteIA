using Pgvector;

namespace AssistenteIA.ApiService.Models;

public class RAGItem
{
    public int Id { get; set; }
    public string Pergunta { get; set; }
    public string Resposta { get; set; }
    public Vector Embedding { get; set; }
    public float Similaridade { get; set; }
}
