using AssistenteIA.ApiService.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Dapper;

namespace AssistenteIA.ApiService.Repositories;

public class MetadataRepository(IConfiguration configuration, ILogger<MetadataRepository> logger) : BaseRepository(configuration)
{
    public async Task<List<Metadata>> ObterMetadata(CancellationToken cancellationToken = default)
    {
        var metadata = new List<Metadata>();
        try
        {
            using var connection = await ObterConexao(cancellationToken);

            string tableQuery = @"
                    SELECT table_name  
                    FROM information_schema.tables 
                    WHERE table_type = 'BASE TABLE' AND table_schema = @schemaName;";

            var tabelas = (await connection.QueryAsync<string>(tableQuery, new { schemaName = SCHEMA })).AsList();

            foreach (var tabela in tabelas)
            {
                metadata.Add(await ObterMetadadosTabela(tabela, connection));
            }

            return metadata;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Erro ao obter esquema do banco de dados.");
            throw;
        }
    }

    private async Task<Metadata> ObterMetadadosTabela(string tabela, NpgsqlConnection connection)
    {
        var metadados = new Metadata
        {
            Tabela = tabela,
            Campos = await ObterColunas(tabela, connection)
        };

        metadados = await ObterChaves(metadados, connection);

        return metadados;
    }

    private async Task<List<Campo>> ObterColunas(string tabela, NpgsqlConnection connection)
    {
        string columnQuery = @"
                SELECT 
                    c.column_name as nome, 
                    c.data_type as tipo, 
                    pgd.description AS descricao
                FROM 
                    information_schema.columns c
                LEFT JOIN pg_catalog.pg_statio_all_tables st 
                    ON c.table_schema = st.schemaname AND c.table_name = st.relname
                LEFT JOIN pg_catalog.pg_description pgd 
                    ON pgd.objoid = st.relid AND pgd.objsubid = c.ordinal_position
                WHERE 
                    c.table_schema = @schemaName AND c.table_name = @tableName;";

        var colunas = await connection.QueryAsync<Campo>(columnQuery, new { schemaName = SCHEMA, tableName = tabela });

        return colunas.AsList();
    }

    private async Task<Metadata> ObterChaves(Metadata metadata, NpgsqlConnection connection)
    {
        string tableConstraintsQuery = @"
                SELECT 
                    tc.constraint_type as constraint_type,
                    kcu.column_name, 
                    ccu.table_name AS foreign_table_name, 
                    ccu.column_name AS foreign_column_name
                FROM information_schema.table_constraints tc 
                JOIN information_schema.key_column_usage kcu
                    ON tc.constraint_name = kcu.constraint_name
                    AND tc.table_schema = kcu.table_schema
                JOIN information_schema.constraint_column_usage ccu
                    ON ccu.constraint_name = tc.constraint_name
                    AND ccu.table_schema = tc.table_schema
                WHERE 
                    tc.table_name = @tableName;";

        var constraints = await connection.QueryAsync<dynamic>(tableConstraintsQuery, new { tableName = metadata.Tabela });

        foreach (var constraint in constraints)
        {
            switch (constraint.constraint_type)
            {
                case "PRIMARY KEY":
                    metadata.PrimaryKeys.Add(constraint.column_name);
                    break;
                case "FOREIGN KEY":
                    metadata.ForeignKeys.Add(new ChaveEstrangeira(
                        constraint.column_name,
                        constraint.foreign_table_name,
                        constraint.foreign_column_name));
                    break;
                case "UNIQUE":
                    metadata.UniqueKeys.Add(constraint.column_name);
                    break;
                default:
                    break;
            }
        }

        return metadata;
    }
}
