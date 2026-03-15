using System.Security.Claims;
using System.Text;
using KF.Web.Authorization.Core;
using KF.Web.Authorization.Dynamic;
using KF.Web.Authorization.Sample.Conditions;
using KF.Web.Authorization.Sample.Controllers;
using KF.Web.Authorization.Sample.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddRoleAuthorizationCore();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<BusinessHoursCondition>();

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection.GetValue<string>("Key") ?? string.Empty;
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection.GetValue<string>("Issuer"),
            ValidAudience = jwtSection.GetValue<string>("Audience"),
            IssuerSigningKey = signingKey,
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();

var dynamicRules = new List<MethodPermissionRule>
{
    new(
        typeFullName: typeof(DynamicDemoController).FullName!,
        methodName: nameof(DynamicDemoController.ViewOrders),
        ruleKind: RoleRuleKind.AnyOf,
        roles: new[] { "Admin", "Sales" }),

    new(
        typeFullName: typeof(DynamicDemoController).FullName!,
        methodName: nameof(DynamicDemoController.CreateOrder),
        ruleKind: RoleRuleKind.AllOf,
        roles: new[] { "Admin", "Sales" },
        condition: static (ctx, user, ct) =>
        {
            var headerValue = ctx.Request.Headers["X-Request-Source"].ToString();
            var allowed = string.Equals(headerValue, "Internal", StringComparison.OrdinalIgnoreCase);
            return ValueTask.FromResult(allowed);
        }),

    new(
        typeFullName: typeof(DynamicDemoController).FullName!,
        methodName: nameof(DynamicDemoController.DeleteOrder),
        ruleKind: RoleRuleKind.AnyOf,
        roles: new[] { "Admin" },
        condition: static (ctx, user, ct) =>
        {
            var hour = DateTimeOffset.UtcNow.Hour;
            var allowed = hour >= 8 && hour <= 17;
            return ValueTask.FromResult(allowed);
        }),

    new(
        typeFullName: typeof(DynamicDemoController).FullName!,
        methodName: nameof(DynamicDemoController.GetSensitiveReport),
        ruleKind: RoleRuleKind.NotAnyOf,
        roles: new[] { "Suspended", "Blacklisted" },
        condition: static (ctx, user, ct) =>
        {
            var tenantFromQuery = ctx.Request.Query["tenantId"].ToString();
            var tenantFromClaim = user.FindFirst("tenant_id")?.Value;
            var allowed = !string.IsNullOrEmpty(tenantFromQuery)
                          && string.Equals(tenantFromQuery, tenantFromClaim, StringComparison.Ordinal);
            return ValueTask.FromResult(allowed);
        })
};

builder.Services.AddDynamicMethodAuthorization(dynamicRules);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseDynamicMethodAuthorization();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
