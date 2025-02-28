using Npgsql;



namespace AssistenteIA.ApiService.Repositories
{
    public abstract class BaseRepository
    {

        protected const string SCHEMA = "public";
        private readonly NpgsqlDataSource _dataSource;
        protected BaseRepository(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' não encontrada.");

            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            dataSourceBuilder.UseVector();

            _dataSource = dataSourceBuilder.Build();
        }
        protected async Task<NpgsqlConnection> ObterConexao(CancellationToken cancellationToken = default)
        {            
            return await _dataSource.OpenConnectionAsync(cancellationToken);
        }
    }
}
