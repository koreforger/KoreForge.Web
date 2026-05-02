using Microsoft.AspNetCore.Mvc;

namespace KoreForge.Web.Authorization.Sample.Controllers;

[ApiController]
[Route("api/dyn")]
public sealed class DynamicDemoController : ControllerBase
{
    [HttpGet("orders/view")]
    public IActionResult ViewOrders() => Ok(new { Message = "Orders retrieved." });

    [HttpPost("orders/create")]
    public IActionResult CreateOrder() => Ok(new { Message = "Order created." });

    [HttpDelete("orders/{id}")]
    public IActionResult DeleteOrder(Guid id) => Ok(new { Message = $"Order {id} deleted." });

    [HttpGet("reports/sensitive")]
    public IActionResult GetSensitiveReport([FromQuery] string tenantId) => Ok(new { Message = $"Sensitive report for {tenantId}." });
}
