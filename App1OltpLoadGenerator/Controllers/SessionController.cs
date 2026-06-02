using Microsoft.AspNetCore.Mvc;
using App1OltpLoadGenerator.Services;
using NewRelic.Api.Agent;

namespace App1OltpLoadGenerator.Controllers;

[ApiController]
[Route("api/sessions")]
public class SessionController : ControllerBase
{
    private readonly SessionService _sessionService;

    public SessionController(SessionService sessionService)
    {
        _sessionService = sessionService;
    }

    [HttpPost("create")]
    [Transaction(Web = true)]
    public IActionResult CreateSession([FromQuery] long customerId)
    {
        try
        {
            string userAgent = "LoadGenerator/1.0";
            _sessionService.CreateSession(customerId, userAgent);
            return Ok(new { message = "Session created" });
        }
        catch (Exception ex)
        {
            NewRelic.Api.Agent.NewRelic.NoticeError(ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
