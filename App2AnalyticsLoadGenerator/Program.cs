using App2AnalyticsLoadGenerator.Services;
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
var maxPoolSize = int.Parse(ExpandEnvironmentVariables(config["Database:MaxPoolSize"] ?? "15"));
var minPoolSize = int.Parse(ExpandEnvironmentVariables(config["Database:MinPoolSize"] ?? "5"));
var threads = int.Parse(ExpandEnvironmentVariables(config["LoadGenerator:Threads"] ?? "10"));
var apiPort = ExpandEnvironmentVariables(Environment.GetEnvironmentVariable("API_PORT") ?? "8081");
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
builder.Services.AddSingleton<CustomerDataService>();
builder.Services.AddSingleton<SalesAnalyticsService>();
builder.Services.AddSingleton<CustomerAnalyticsService>();
builder.Services.AddSingleton<ProductAnalyticsService>();
builder.Services.AddSingleton<ReportingService>();
builder.Services.AddSingleton<DataWarehouseService>();

// Register load generator
builder.Services.AddSingleton(sp => new AnalyticsLoadGenerator(
    sp.GetRequiredService<IHttpClientFactory>(),
    apiBaseUrl,
    threads
));

var app = builder.Build();

// Configure the HTTP request pipeline
app.MapControllers();

// Start load generator in background
var loadGenerator = app.Services.GetRequiredService<AnalyticsLoadGenerator>();
_ = Task.Run(() => loadGenerator.Start());

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
