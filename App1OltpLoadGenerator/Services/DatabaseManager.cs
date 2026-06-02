using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace App1OltpLoadGenerator.Services;

public class DatabaseManager : IDisposable
{
    private readonly string _connectionString;
    private readonly int _maxPoolSize;
    private readonly int _minPoolSize;
    private readonly ILogger<DatabaseManager>? _logger;

    public DatabaseManager(string connectionString, int maxPoolSize, int minPoolSize)
    {
        _connectionString = connectionString;
        _maxPoolSize = maxPoolSize;
        _minPoolSize = minPoolSize;

        // Configure Oracle connection pooling
        ConfigureConnectionPool();
    }

    private void ConfigureConnectionPool()
    {
        // Oracle.ManagedDataAccess.Core uses connection string for pool configuration
        var builder = new OracleConnectionStringBuilder(_connectionString)
        {
            Pooling = true,
            MaxPoolSize = _maxPoolSize,
            MinPoolSize = _minPoolSize,
            ConnectionTimeout = 30,
            ValidateConnection = true,
            // Oracle-specific timeouts - CRITICAL for long-running queries
            // Note: Statement timeout is set per-command, not connection
        };

        _connectionString = builder.ConnectionString;
    }

    public OracleConnection GetConnection()
    {
        var connection = new OracleConnection(_connectionString);
        connection.Open();

        // Set network timeout for the connection (60 seconds for OLTP)
        // This is equivalent to Java's conn.setNetworkTimeout(null, 60000)
        // In .NET, we handle this at the command level with CommandTimeout

        return connection;
    }

    public void Dispose()
    {
        // Connection pooling is managed by Oracle.ManagedDataAccess.Core
        // Clearing the pool on dispose
        OracleConnection.ClearAllPools();
    }
}
