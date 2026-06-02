using Oracle.ManagedDataAccess.Client;

namespace App2AnalyticsLoadGenerator.Services;

public class DatabaseManager : IDisposable
{
    private readonly string _connectionString;
    private readonly int _maxPoolSize;
    private readonly int _minPoolSize;

    public DatabaseManager(string connectionString, int maxPoolSize, int minPoolSize)
    {
        _connectionString = connectionString;
        _maxPoolSize = maxPoolSize;
        _minPoolSize = minPoolSize;

        ConfigureConnectionPool();
    }

    private void ConfigureConnectionPool()
    {
        // Analytics queries need longer timeouts (3 minutes)
        var builder = new OracleConnectionStringBuilder(_connectionString)
        {
            Pooling = true,
            MaxPoolSize = _maxPoolSize,  // Smaller pool for analytics (15)
            MinPoolSize = _minPoolSize,   // 5
            ConnectionTimeout = 30,
            ValidateConnection = true
        };

        _connectionString = builder.ConnectionString;
    }

    public OracleConnection GetConnection()
    {
        var connection = new OracleConnection(_connectionString);
        connection.Open();
        return connection;
    }

    public void Dispose()
    {
        OracleConnection.ClearAllPools();
    }
}
