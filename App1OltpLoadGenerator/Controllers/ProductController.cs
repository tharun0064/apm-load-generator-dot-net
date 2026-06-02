using Microsoft.AspNetCore.Mvc;
using App1OltpLoadGenerator.Services;
using NewRelic.Api.Agent;

namespace App1OltpLoadGenerator.Controllers;

[ApiController]
[Route("api/products")]
public class ProductController : ControllerBase
{
    private readonly ProductService _productService;

    public ProductController(ProductService productService)
    {
        _productService = productService;
    }

    [HttpGet("{productId}")]
    [Transaction(Web = true)]
    public ActionResult<Dictionary<string, object>> GetProduct(long productId)
    {
        try
        {
            var product = _productService.GetProductDetails(productId);
            return product != null ? Ok(product) : NotFound();
        }
        catch (Exception ex)
        {
            NewRelic.Api.Agent.NewRelic.NoticeError(ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
