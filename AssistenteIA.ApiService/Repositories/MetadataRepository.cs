using AssistenteIA.ApiService.Models;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Npgsql;
using System.Linq;
using System.Text;

namespace AssistenteIA.ApiService.Repositories;

public class MetadataRepository(IConfiguration configuration, ILogger<MetadataRepository> logger): BaseRepository(configuration)
{
    public async Task<List<Metadata>> ObterMetadata()
    {
        var metadata = new List<Metadata>();
        try
        {
            await using var connection = ObterConexao();
            await connection.OpenAsync();

            string tableQuery = @"SELECT table_name  
                                  FROM information_schema.tables 
                                  WHERE table_type = 'BASE TABLE' AND table_schema = @schemaName;";

            await using var tableCmd = new NpgsqlCommand(tableQuery, connection);
            tableCmd.Parameters.AddWithValue("schemaName", SCHEMA);

            var tabelas = new List<string>();
            
            await using (var reader = await tableCmd.ExecuteReaderAsync()) 
            {
                while (await reader.ReadAsync())
                    tabelas.Add(reader.GetString(0));
            } 


            foreach (var tabela in tabelas)
                metadata.Add(await ObterMetadadosTabela(tabela, connection));               

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
        var colunas = new List<Campo>();

        string columnQuery = @"
                SELECT 
                    c.column_name, 
                    c.data_type, 
                    pgd.description AS column_comment
                FROM 
                    information_schema.columns c
                LEFT JOIN pg_catalog.pg_statio_all_tables st 
                    ON c.table_schema = st.schemaname AND c.table_name = st.relname
                LEFT JOIN pg_catalog.pg_description pgd 
                    ON pgd.objoid = st.relid AND pgd.objsubid = c.ordinal_position
                WHERE 
                    c.table_schema = @schemaName AND c.table_name = @tableName;
            ";

        await using var colCmd = new NpgsqlCommand(columnQuery, connection);
        colCmd.Parameters.AddWithValue("schemaName", SCHEMA);
        colCmd.Parameters.AddWithValue("tableName", tabela);
        await using var colReader = await colCmd.ExecuteReaderAsync();

        while (await colReader.ReadAsync())
        {
            string nome = colReader.GetString(0);
            string tipo = colReader.GetString(1);
            string descricao = colReader.IsDBNull(2) ? "" : colReader.GetString(2);

            colunas.Add(new Campo(nome, tipo, descricao));
        }

        return colunas;
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
                    tc.table_name = @tableName;
            ";

        await using var constraintsCmd = new NpgsqlCommand(tableConstraintsQuery, connection);
        constraintsCmd.Parameters.AddWithValue("tableName", metadata.Tabela);

        await using var constraintReader = await constraintsCmd.ExecuteReaderAsync();
        if (constraintReader.HasRows)
        {
            while (await constraintReader.ReadAsync())
            {
                switch (constraintReader.GetString(0))
                {
                    case "PRIMARY KEY":
                        metadata.PrimaryKeys.Add(constraintReader.GetString(1));
                        break;
                    case "FOREIGN KEY":
                        metadata.ForeignKeys.Add(new ChaveEstrangeira(constraintReader.GetString(1), constraintReader.GetString(2), constraintReader.GetString(3)));
                        break;

                    case "UNIQUE":
                        metadata.UniqueKeys.Add(constraintReader.GetString(1));
                        break;

                    default:
                        break;
                }
            }
        }

        return metadata;
    }

}
