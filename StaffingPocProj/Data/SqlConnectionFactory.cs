using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace StaffingPocProj.Data;

public sealed class SqlConnectionFactory(IConfiguration configuration) : IDbConnectionFactory
{
    private readonly string _connectionString =
        configuration.GetConnectionString("Stm")
        ?? throw new InvalidOperationException("Connection string 'Stm' is missing in appsettings.");

    public DbConnection Create() => new SqlConnection(_connectionString);
}