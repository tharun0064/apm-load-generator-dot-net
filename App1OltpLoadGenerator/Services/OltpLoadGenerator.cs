using NewRelic.Api.Agent;

namespace App1OltpLoadGenerator.Services;

public class OltpLoadGenerator
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiBaseUrl;
    private readonly int _numThreads;
    private readonly Random _random = new();
    private volatile bool _running = true;
    private readonly ILogger<OltpLoadGenerator>? _logger;

    public OltpLoadGenerator(IHttpClientFactory httpClientFactory, string apiBaseUrl, int numThreads)
    {
        _httpClientFactory = httpClientFactory;
        _apiBaseUrl = apiBaseUrl;
        _numThreads = numThreads;
    }

    [Trace(Dispatcher = true)]
    public void Start()
    {
        Console.WriteLine($"Starting OLTP Load Generator with {_numThreads} threads...");

        var tasks = new List<Task>();
        for (int i = 0; i < _numThreads; i++)
        {
            int threadId = i;
            tasks.Add(Task.Run(() => RunWorker(threadId)));
        }

        Task.WaitAll(tasks.ToArray());
    }

    [Trace(Dispatcher = true)]
    private async Task RunWorker(int threadId)
    {
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(120);

        Console.WriteLine($"Worker thread {threadId} started");

        int iterationCount = 0;
        DateTime lastBreakTime = DateTime.UtcNow;

        while (_running)
        {
            try
            {
                // Weighted operation distribution matching Java version
                int operation = _random.Next(100);

                if (operation < 30)
                {
                    // 30% - Create orders
                    await CreateOrderWorkflow(client);
                }
                else if (operation < 55)
                {
                    // 25% - Update customer loyalty
                    await UpdateCustomerLoyaltyWorkflow(client);
                }
                else if (operation < 75)
                {
                    // 20% - Check/update inventory
                    await InventoryCheckWorkflow(client);
                }
                else if (operation < 90)
                {
                    // 15% - Process transactions
                    await ProcessTransactionWorkflow(client);
                }
                else if (operation < 95)
                {
                    // 5% - Session management
                    await SessionManagementWorkflow(client);
                }
                else if (operation < 98)
                {
                    // 3% - Delete old data
                    await DeleteOldDataWorkflow(client);
                }
                else if (operation < 99)
                {
                    // 1% - Bulk insert
                    await BulkInsertWorkflow(client);
                }
                else
                {
                    // 1% - Product operations
                    await ProductOperationsWorkflow(client);
                }

                // 30-80ms delay = MODERATE-HEAVY LOAD
                await Task.Delay(_random.Next(50) + 30);

                iterationCount++;

                // Take 5-second break every 2-3 minutes (sustainable load)
                if ((DateTime.UtcNow - lastBreakTime).TotalMinutes >= (_random.Next(1) + 2))
                {
                    Console.WriteLine($"Thread {threadId} taking a 5-second break after {iterationCount} iterations");
                    await Task.Delay(5000);
                    lastBreakTime = DateTime.UtcNow;
                    iterationCount = 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in worker thread {threadId}: {ex.Message}");
                await Task.Delay(1000); // Back off on error
            }
        }
    }

    [Trace]
    private async Task CreateOrderWorkflow(HttpClient client)
    {
        long customerId = _random.Next(1000) + 1;
        int numItems = _random.Next(5) + 1;
        string url = $"{_apiBaseUrl}/api/orders/create?customerId={customerId}&numItems={numItems}";

        try
        {
            var response = await client.PostAsync(url, null);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception)
        {
            // Silently ignore - expected under heavy load
        }
    }

    [Trace]
    private async Task UpdateCustomerLoyaltyWorkflow(HttpClient client)
    {
        long customerId = _random.Next(1000) + 1;
        int points = _random.Next(100) + 1;
        string url = $"{_apiBaseUrl}/api/customers/update-loyalty?customerId={customerId}&points={points}";

        try
        {
            var response = await client.PutAsync(url, null);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception)
        {
            // Silently ignore
        }
    }

    [Trace]
    private async Task InventoryCheckWorkflow(HttpClient client)
    {
        long productId = _random.Next(500) + 1;
        string url = $"{_apiBaseUrl}/api/inventory/check?productId={productId}";

        try
        {
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception)
        {
            // Silently ignore
        }
    }

    [Trace]
    private async Task ProcessTransactionWorkflow(HttpClient client)
    {
        long orderId = _random.Next(10000) + 1;
        string url = $"{_apiBaseUrl}/api/transactions/process?orderId={orderId}";

        try
        {
            var response = await client.PostAsync(url, null);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception)
        {
            // Silently ignore
        }
    }

    [Trace]
    private async Task SessionManagementWorkflow(HttpClient client)
    {
        long customerId = _random.Next(1000) + 1;
        string url = $"{_apiBaseUrl}/api/sessions/create?customerId={customerId}";

        try
        {
            var response = await client.PostAsync(url, null);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception)
        {
            // Silently ignore
        }
    }

    [Trace]
    private async Task DeleteOldDataWorkflow(HttpClient client)
    {
        int daysToKeep = 90;
        string url = $"{_apiBaseUrl}/api/orders/delete-old?daysToKeep={daysToKeep}";

        try
        {
            var response = await client.DeleteAsync(url);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception)
        {
            // Silently ignore
        }
    }

    [Trace]
    private async Task BulkInsertWorkflow(HttpClient client)
    {
        int count = 50;
        string url = $"{_apiBaseUrl}/api/inventory/bulk-update?count={count}";

        try
        {
            var response = await client.PutAsync(url, null);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception)
        {
            // Silently ignore
        }
    }

    [Trace]
    private async Task ProductOperationsWorkflow(HttpClient client)
    {
        long productId = _random.Next(500) + 1;
        string url = $"{_apiBaseUrl}/api/products/{productId}";

        try
        {
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception)
        {
            // Silently ignore
        }
    }

    public void Stop()
    {
        _running = false;
    }
}
