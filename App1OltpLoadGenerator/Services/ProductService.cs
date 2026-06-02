using Oracle.ManagedDataAccess.Client;
using NewRelic.Api.Agent;

namespace App1OltpLoadGenerator.Services;

public class ProductService
{
    private readonly DatabaseManager _dbManager;

    public ProductService(DatabaseManager dbManager)
    {
        _dbManager = dbManager;
    }

    [Trace]
    public Dictionary<string, object>? GetProductDetails(long productId)
    {
        using var conn = _dbManager.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandTimeout = 60;

        cmd.CommandText = @"
            SELECT product_id, product_name, category, subcategory, price, cost, is_active
            FROM PRODUCTS
            WHERE product_id = :productId";

        cmd.Parameters.Add("productId", OracleDbType.Int64).Value = productId;

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new Dictionary<string, object>
            {
                ["product_id"] = reader.GetInt64(0),
                ["product_name"] = reader.GetString(1),
                ["category"] = reader.GetString(2),
                ["subcategory"] = reader.GetString(3),
                ["price"] = reader.GetDecimal(4),
                ["cost"] = reader.GetDecimal(5),
                ["is_active"] = reader.GetInt32(6)
            };
        }

        return null;
    }

    [Trace]
    public void UpdateProductPrice(long productId, decimal newPrice)
    {
        using var conn = _dbManager.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandTimeout = 60;

        cmd.CommandText = "UPDATE PRODUCTS SET price = :newPrice WHERE product_id = :productId";
        cmd.Parameters.Add("newPrice", OracleDbType.Decimal).Value = newPrice;
        cmd.Parameters.Add("productId", OracleDbType.Int64).Value = productId;

        cmd.ExecuteNonQuery();
    }
}
