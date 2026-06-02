using Microsoft.AspNetCore.Mvc;
using App1OltpLoadGenerator.Services;
using NewRelic.Api.Agent;

namespace App1OltpLoadGenerator.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomerController : ControllerBase
{
    private readonly CustomerService _customerService;

    public CustomerController(CustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpPut("update-loyalty")]
    [Transaction(Web = true)]
    public IActionResult UpdateLoyalty([FromQuery] long customerId, [FromQuery] int points)
    {
        NewRelic.Api.Agent.NewRelic.AddCustomParameter("customerId", customerId);
        NewRelic.Api.Agent.NewRelic.AddCustomParameter("points", points);

        try
        {
            _customerService.UpdateLoyaltyPoints(customerId, points);
            return Ok(new { message = "Loyalty points updated" });
        }
        catch (Exception ex)
        {
            NewRelic.Api.Agent.NewRelic.NoticeError(ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("{customerId}")]
    [Transaction(Web = true)]
    public ActionResult<Dictionary<string, object>> GetCustomer(long customerId)
    {
        try
        {
            var customer = _customerService.GetCustomerDetails(customerId);
            return customer != null ? Ok(customer) : NotFound();
        }
        catch (Exception ex)
        {
            NewRelic.Api.Agent.NewRelic.NoticeError(ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
