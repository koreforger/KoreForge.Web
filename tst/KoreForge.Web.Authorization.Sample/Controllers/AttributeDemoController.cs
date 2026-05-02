using KoreForge.Web.Authorization.Core;
using KoreForge.Web.Authorization.Core.Mvc;
using KoreForge.Web.Authorization.Sample.Conditions;
using Microsoft.AspNetCore.Mvc;

namespace KoreForge.Web.Authorization.Sample.Controllers;

[ApiController]
[Route("api/attr")]
public sealed class AttributeDemoController : ControllerBase
{
    [HttpGet("admin-or-support")]
    [RolesAuthorize(RoleRuleKind.AnyOf, new[] { "Admin", "Support" })]
    public IActionResult AdminOrSupport() => Ok("Admin or Support endpoint accessed.");

    [HttpGet("admin-and-supervisor")]
    [RolesAuthorize(RoleRuleKind.AllOf, new[] { "Admin", "Supervisor" })]
    public IActionResult AdminAndSupervisor() => Ok("Admin + Supervisor endpoint accessed.");

    [HttpGet("everyone-except-suspended")]
    [RolesAuthorize(RoleRuleKind.NotAnyOf, new[] { "Suspended" })]
    public IActionResult EveryoneExceptSuspended() => Ok("All non-suspended users welcome.");

    [HttpGet("not-trader-and-auditor")]
    [RolesAuthorize(RoleRuleKind.NotAllOf, new[] { "Trader", "Auditor" })]
    public IActionResult NotTraderAndAuditor() => Ok("Everyone except Trader+Auditor combo.");

    [HttpGet("business-hours-only")]
    [RolesAuthorize(RoleRuleKind.AnyOf, new[] { "User", "Admin" }, typeof(BusinessHoursCondition))]
    public IActionResult BusinessHoursOnly() => Ok("Business hours check passed.");
}
