using Oracle.ManagedDataAccess.Client;
using NewRelic.Api.Agent;

namespace App1OltpLoadGenerator.Services;

public class TransactionService
{
    private readonly DatabaseManager _dbManager;

    public TransactionService(DatabaseManager dbManager)
    {
        _dbManager = dbManager;
    }

    [Trace]
    public void CreateTransaction(long orderId, decimal amount, string paymentMethod, string paymentGateway)
    {
        using var conn = _dbManager.GetConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandTimeout = 60;

        cmd.CommandText = @"
            INSERT INTO TRANSACTIONS (transaction_id, order_id, amount, payment_method, payment_gateway, transaction_type, status, processed_at)
            VALUES (transaction_seq.NEXTVAL, :orderId, :amount, :paymentMethod, :paymentGateway, 'PAYMENT', 'COMPLETED', CURRENT_TIMESTAMP)";

        cmd.Parameters.Add("orderId", OracleDbType.Int64).Value = orderId;
        cmd.Parameters.Add("amount", OracleDbType.Decimal).Value = amount;
        cmd.Parameters.Add("paymentMethod", OracleDbType.Varchar2).Value = paymentMethod;
        cmd.Parameters.Add("paymentGateway", OracleDbType.Varchar2).Value = paymentGateway;

        cmd.ExecuteNonQuery();
    }

    [Trace]
    public void ProcessTransaction(long orderId)
    {
        var random = new Random();
        string[] gateways = { "STRIPE", "PAYPAL", "SQUARE" };
        string gateway = gateways[random.Next(gateways.Length)];

        CreateTransaction(orderId, random.Next(50, 500), "CREDIT_CARD", gateway);
    }
}
