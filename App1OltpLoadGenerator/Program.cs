using App1OltpLoadGenerator.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Load .env file if it exists
DotNetEnv.Env.Load();

// Replace placeholders in configuration with environment variables
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();

// Expand environment variable placeholders
var connectionString = ExpandEnvironmentVariables(config.GetConnectionString("OracleDb") ?? "");
var maxPoolSize = int.Parse(ExpandEnvironmentVariables(config["Database:MaxPoolSize"] ?? "100"));
var minPoolSize = int.Parse(ExpandEnvironmentVariables(config["Database:MinPoolSize"] ?? "20"));
var threads = int.Parse(ExpandEnvironmentVariables(config["LoadGenerator:Threads"] ?? "3"));
var apiPort = ExpandEnvironmentVariables(Environment.GetEnvironmentVariable("API_PORT") ?? "8080");
var apiBaseUrl = $"http://localhost:{apiPort}";

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddHttpClient();

// Register database and service layer
builder.Services.AddSingleton(new DatabaseManager(connectionString, maxPoolSize, minPoolSize));
builder.Services.AddSingleton<OrderService>();
builder.Services.AddSingleton<CustomerService>();
builder.Services.AddSingleton<ProductService>();
builder.Services.AddSingleton<InventoryService>();
builder.Services.AddSingleton<TransactionService>();
builder.Services.AddSingleton<SessionService>();
builder.Services.AddSingleton<TableCleanupService>();

// Register load generator
builder.Services.AddSingleton(sp => new OltpLoadGenerator(
    sp.GetRequiredService<IHttpClientFactory>(),
    apiBaseUrl,
    threads
));

var app = builder.Build();

// Configure the HTTP request pipeline
app.MapControllers();

// Start table cleanup and load generation on startup
var cleanup = app.Services.GetRequiredService<TableCleanupService>();
var loadGenerator = app.Services.GetRequiredService<OltpLoadGenerator>();

// Initial cleanup
await Task.Run(() => cleanup.TruncateAndRebuild());

// Start load generator in background
_ = Task.Run(() => loadGenerator.Start());

// Schedule periodic cleanup every 35 minutes
var timer = new System.Timers.Timer(35 * 60 * 1000);
timer.Elapsed += (sender, e) => {
    try {
        cleanup.TruncateAndRebuild();
    } catch (Exception ex) {
        Log.Error(ex, "Error during scheduled cleanup");
    }
};
timer.Start();

app.Run();

static string ExpandEnvironmentVariables(string value)
{
    if (string.IsNullOrEmpty(value)) return value;

    // Replace ${VAR_NAME} with environment variable value
    return System.Text.RegularExpressions.Regex.Replace(value, @"\$\{(\w+)\}", match =>
    {
        var varName = match.Groups[1].Value;
        return Environment.GetEnvironmentVariable(varName) ?? match.Value;
    });
}
