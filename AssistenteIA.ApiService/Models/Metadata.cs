namespace AssistenteIA.ApiService.Models;

public class Metadata
{
    public string Tabela { get; set; }
    public List<Campo> Campos { get; set; }

    public List<string> PrimaryKeys { get; set; } = [];

    public List<ChaveEstrangeira> ForeignKeys { get; set; } = [];

    public List<string> UniqueKeys { get; set; } = [];

}

public class Campo(string nome, string tipo, string descricao)
{
    public string Nome { get; set; } = nome;
    public string Tipo { get; set; } = tipo;
    public string Descricao { get; set; } = descricao;
}
public class ChaveEstrangeira(string coluna, string tabelaReferenciada, string colunaReferenciada)
{    
    public string Coluna { get; set; } = coluna;
    public string TabelaReferenciada { get; set; } = tabelaReferenciada;
    public string ColunaReferenciada { get; set; } = colunaReferenciada;
};
