using Dapper;
using Npgsql;
using Pgvector.Dapper;
using System.Threading.Tasks;



namespace AssistenteIA.ApiService.Repositories
{
    public abstract class BaseRepository(IConfiguration configuration)
    {
        protected readonly string _connectionString = configuration.GetConnectionString("DefaultConnection");
        
        protected const string SCHEMA = "public";
        protected async Task<NpgsqlConnection> ObterConexao(CancellationToken cancellationToken = default)
        {
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(_connectionString);
            dataSourceBuilder.UseVector();
            var source = dataSourceBuilder.Build();     
            
            return await source.OpenConnectionAsync(cancellationToken);
        }
    }
}
