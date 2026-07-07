using NewRelic.Api.Agent;

namespace App2AnalyticsLoadGenerator.Services;

public class AnalyticsLoadGenerator
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiBaseUrl;
    private readonly int _numThreads;
    private readonly Random _random = new();
    private volatile bool _running = true;

    public AnalyticsLoadGenerator(IHttpClientFactory httpClientFactory, string apiBaseUrl, int numThreads)
    {
        _httpClientFactory = httpClientFactory;
        _apiBaseUrl = apiBaseUrl;
        _numThreads = numThreads;
    }

    [Transaction]
    public void Start()
    {
        Console.WriteLine($"Starting Analytics Load Generator with {_numThreads} threads...");

        var tasks = new List<Task>();
        for (int i = 0; i < _numThreads; i++)
        {
            int threadId = i;
            tasks.Add(Task.Run(() => RunWorker(threadId)));
        }

        Task.WaitAll(tasks.ToArray());
    }

    [Transaction]
    private async Task RunWorker(int threadId)
    {
        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(5); // Long timeout for heavy queries

        Console.WriteLine($"Analytics worker thread {threadId} started");

        int iterationCount = 0;
        DateTime lastBreakTime = DateTime.UtcNow;

        while (_running)
        {
            try
            {
                // ALWAYS call customer-data API first (the heaviest query)
                await CallCustomerDataAPI(client);

                // Then randomly select other operations: 25% sales, 20% customer, 20% product, 20% reporting, 15% warehouse
                int operation = _random.Next(100);

                if (operation < 25)
                {
                    await CallSalesAnalyticsAPI(client);
                }
                else if (operation < 45)
                {
                    await CallCustomerAnalyticsAPI(client);
                }
                else if (operation < 65)
                {
                    await CallProductAnalyticsAPI(client);
                }
                else if (operation < 85)
                {
                    await CallReportingAPI(client);
                }
                else
                {
                    await CallDataWarehouseAPI(client);
                }

                // 8-23ms delay between operations
                await Task.Delay(_random.Next(15) + 8);

                iterationCount++;

                // Take 3-second break every 3-5 minutes
                if ((DateTime.UtcNow - lastBreakTime).TotalMinutes >= (_random.Next(2) + 3))
                {
                    Console.WriteLine($"Analytics thread {threadId} taking a 3-second break after {iterationCount} iterations");
                    await Task.Delay(3000);
                    lastBreakTime = DateTime.UtcNow;
                    iterationCount = 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in analytics worker thread {threadId}: {ex.Message}");
                await Task.Delay(1000);
            }
        }
    }

    [Trace]
    private async Task CallCustomerDataAPI(HttpClient client)
    {
        try
        {
            var response = await client.GetAsync($"{_apiBaseUrl}/api/analytics/customer-data");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception) { }
    }

    [Trace]
    private async Task CallSalesAnalyticsAPI(HttpClient client)
    {
        string[] endpoints = { "daily-sales", "monthly-sales", "top-products", "revenue-by-payment" };
        string endpoint = endpoints[_random.Next(endpoints.Length)];

        try
        {
            var response = await client.GetAsync($"{_apiBaseUrl}/api/analytics/sales/{endpoint}");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception) { }
    }

    [Trace]
    private async Task CallCustomerAnalyticsAPI(HttpClient client)
    {
        string[] endpoints = { "customer-segmentation", "customer-ltv", "retention-rate", "purchase-frequency" };
        string endpoint = endpoints[_random.Next(endpoints.Length)];

        try
        {
            var response = await client.GetAsync($"{_apiBaseUrl}/api/analytics/customer/{endpoint}");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception) { }
    }

    [Trace]
    private async Task CallProductAnalyticsAPI(HttpClient client)
    {
        string[] endpoints = { "product-performance", "inventory-turnover", "slow-moving", "profit-margin", "affinity", "revenue-growth" };
        string endpoint = endpoints[_random.Next(endpoints.Length)];

        try
        {
            var response = await client.GetAsync($"{_apiBaseUrl}/api/analytics/product/{endpoint}");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception) { }
    }

    [Trace]
    private async Task CallReportingAPI(HttpClient client)
    {
        string[] endpoints = { "executive-dashboard", "sales-report", "inventory-report", "customer-report", "transaction-report" };
        string endpoint = endpoints[_random.Next(endpoints.Length)];

        try
        {
            var response = await client.GetAsync($"{_apiBaseUrl}/api/reporting/{endpoint}");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception) { }
    }

    [Trace]
    private async Task CallDataWarehouseAPI(HttpClient client)
    {
        string[] endpoints = { "aggregate-sales", "aggregate-customers", "aggregate-products", "full-table-scan", "complex-join" };
        string endpoint = endpoints[_random.Next(endpoints.Length)];

        try
        {
            var response = await client.PostAsync($"{_apiBaseUrl}/api/warehouse/{endpoint}", null);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception) { }
    }

    public void Stop()
    {
        _running = false;
    }
}
