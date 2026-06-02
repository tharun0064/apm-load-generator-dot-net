using Microsoft.AspNetCore.Mvc;
using App1OltpLoadGenerator.Services;
using NewRelic.Api.Agent;

namespace App1OltpLoadGenerator.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly InventoryService _inventoryService;

    public InventoryController(InventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet("check")]
    [Transaction(Web = true)]
    public ActionResult<Dictionary<string, int>> CheckInventory([FromQuery] long productId)
    {
        try
        {
            int quantity = _inventoryService.CheckInventory(productId);
            return Ok(new Dictionary<string, int> { ["quantity"] = quantity });
        }
        catch (Exception ex)
        {
            NewRelic.Api.Agent.NewRelic.NoticeError(ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPut("bulk-update")]
    [Transaction(Web = true)]
    public IActionResult BulkUpdate([FromQuery] int count = 50)
    {
        try
        {
            _inventoryService.BulkUpdateInventory(count);
            return Ok(new { message = "Bulk update completed" });
        }
        catch (Exception ex)
        {
            NewRelic.Api.Agent.NewRelic.NoticeError(ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
