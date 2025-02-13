using Npgsql;

namespace AssistenteIA.ApiService.Repositories
{
    public abstract class BaseRepository(IConfiguration configuration)
    {
        protected readonly string _connectionString = configuration.GetConnectionString("DefaultConnection");
        
        protected const string SCHEMA = "public";
        protected NpgsqlConnection ObterConexao()
        {
            return new NpgsqlConnection(_connectionString);
        }
    }
}
