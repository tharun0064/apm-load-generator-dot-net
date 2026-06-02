using Microsoft.AspNetCore.Mvc;
using App2AnalyticsLoadGenerator.Services;
using NewRelic.Api.Agent;

namespace App2AnalyticsLoadGenerator.Controllers;

[ApiController]
[Route("api")]
public class AnalyticsController : ControllerBase
{
    private readonly CustomerDataService _customerDataService;
    private readonly SalesAnalyticsService _salesAnalyticsService;
    private readonly CustomerAnalyticsService _customerAnalyticsService;
    private readonly ProductAnalyticsService _productAnalyticsService;
    private readonly ReportingService _reportingService;
    private readonly DataWarehouseService _dataWarehouseService;

    public AnalyticsController(
        CustomerDataService customerDataService,
        SalesAnalyticsService salesAnalyticsService,
        CustomerAnalyticsService customerAnalyticsService,
        ProductAnalyticsService productAnalyticsService,
        ReportingService reportingService,
        DataWarehouseService dataWarehouseService)
    {
        _customerDataService = customerDataService;
        _salesAnalyticsService = salesAnalyticsService;
        _customerAnalyticsService = customerAnalyticsService;
        _productAnalyticsService = productAnalyticsService;
        _reportingService = reportingService;
        _dataWarehouseService = dataWarehouseService;
    }

    [HttpGet("analytics/customer-data")]
    [Transaction(Web = true)]
    public IActionResult GetCustomerData()
    {
        try
        {
            _customerDataService.GetCustomerAnalytics();
            return Ok(new { message = "Query executed" });
        }
        catch (Exception ex)
        {
            NewRelic.Api.Agent.NewRelic.NoticeError(ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("analytics/sales/{operation}")]
    [Transaction(Web = true)]
    public IActionResult GetSalesAnalytics(string operation)
    {
        try
        {
            switch (operation)
            {
                case "daily-sales":
                    _salesAnalyticsService.GetDailySalesSummary();
                    break;
                case "monthly-sales":
                    _salesAnalyticsService.GetMonthlySalesSummary();
                    break;
            }
            return Ok(new { message = "Query executed" });
        }
        catch (Exception ex)
        {
            NewRelic.Api.Agent.NewRelic.NoticeError(ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("analytics/customer/{operation}")]
    [Transaction(Web = true)]
    public IActionResult GetCustomerAnalytics(string operation)
    {
        try
        {
            if (operation == "retention-rate")
            {
                _customerAnalyticsService.GetCustomerRetentionRate();
            }
            return Ok(new { message = "Query executed" });
        }
        catch (Exception ex)
        {
            NewRelic.Api.Agent.NewRelic.NoticeError(ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("analytics/product/{operation}")]
    [Transaction(Web = true)]
    public IActionResult GetProductAnalytics(string operation)
    {
        try
        {
            if (operation == "product-performance")
            {
                _productAnalyticsService.GetProductPerformanceReport();
            }
            return Ok(new { message = "Query executed" });
        }
        catch (Exception ex)
        {
            NewRelic.Api.Agent.NewRelic.NoticeError(ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("reporting/{operation}")]
    [Transaction(Web = true)]
    public IActionResult GetReporting(string operation)
    {
        try
        {
            if (operation == "executive-dashboard")
            {
                _reportingService.GenerateExecutiveDashboard();
            }
            return Ok(new { message = "Report generated" });
        }
        catch (Exception ex)
        {
            NewRelic.Api.Agent.NewRelic.NoticeError(ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("warehouse/{operation}")]
    [Transaction(Web = true)]
    public IActionResult GetDataWarehouse(string operation)
    {
        try
        {
            switch (operation)
            {
                case "aggregate-sales":
                    _dataWarehouseService.AggregateSalesData();
                    break;
                case "complex-join":
                    _dataWarehouseService.PerformComplexJoinQuery();
                    break;
            }
            return Ok(new { message = "Operation completed" });
        }
        catch (Exception ex)
        {
            NewRelic.Api.Agent.NewRelic.NoticeError(ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
