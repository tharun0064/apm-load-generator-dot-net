using Microsoft.AspNetCore.Mvc;
using App1OltpLoadGenerator.Services;
using NewRelic.Api.Agent;

namespace App1OltpLoadGenerator.Controllers;

[ApiController]
[Route("api/orders")]
public class OrderController : ControllerBase
{
    private readonly OrderService _orderService;
    private readonly CustomerService _customerService;
    private readonly ProductService _productService;
    private readonly InventoryService _inventoryService;
    private readonly TransactionService _transactionService;
    private readonly Random _random = new();

    public OrderController(
        OrderService orderService,
        CustomerService customerService,
        ProductService productService,
        InventoryService inventoryService,
        TransactionService transactionService)
    {
        _orderService = orderService;
        _customerService = customerService;
        _productService = productService;
        _inventoryService = inventoryService;
        _transactionService = transactionService;
    }

    [HttpPost("create")]
    [Transaction(Web = true)]
    public ActionResult<Dictionary<string, object>> CreateOrder([FromQuery] long customerId, [FromQuery] int numItems)
    {
        NewRelic.Api.Agent.NewRelic.GetAgent().CurrentTransaction.AddCustomAttribute("customerId", customerId);
        NewRelic.Api.Agent.NewRelic.GetAgent().CurrentTransaction.AddCustomAttribute("numItems", numItems);

        try
        {
            // Create order
            long orderId = _orderService.CreateOrder(customerId);

            // Add random items
            decimal totalAmount = 0;
            for (int i = 0; i < numItems; i++)
            {
                long productId = _random.Next(500) + 1;
                var product = _productService.GetProductDetails(productId);
                if (product != null)
                {
                    int quantity = _random.Next(3) + 1;
                    decimal price = (decimal)product["price"];
                    _orderService.AddOrderItem(orderId, productId, quantity, price);
                    _inventoryService.ReserveInventory(productId, quantity);
                    totalAmount += quantity * price;
                }
            }

            // Calculate tax and shipping
            decimal taxAmount = totalAmount * 0.08m;
            decimal shippingCost = totalAmount > 100 ? 0 : 10m;
            totalAmount += taxAmount + shippingCost;

            // Update order total
            _orderService.UpdateOrderTotal(orderId, totalAmount, taxAmount, shippingCost);

            // Create transaction
            _transactionService.CreateTransaction(orderId, totalAmount, "CREDIT_CARD", "STRIPE");

            // Update order status
            _orderService.UpdateOrderStatus(orderId, "COMPLETED");

            return Ok(new Dictionary<string, object>
            {
                ["order_id"] = orderId,
                ["customer_id"] = customerId,
                ["total_amount"] = totalAmount,
                ["status"] = "COMPLETED"
            });
        }
        catch (Exception ex)
        {
            NewRelic.Api.Agent.NewRelic.NoticeError(ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("delete-old")]
    [Transaction(Web = true)]
    public ActionResult<Dictionary<string, int>> DeleteOldOrders([FromQuery] int daysToKeep = 90)
    {
        try
        {
            int deletedCount = _orderService.DeleteOldCompletedOrders(daysToKeep);
            return Ok(new Dictionary<string, int> { ["deleted_count"] = deletedCount });
        }
        catch (Exception ex)
        {
            NewRelic.Api.Agent.NewRelic.NoticeError(ex);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
