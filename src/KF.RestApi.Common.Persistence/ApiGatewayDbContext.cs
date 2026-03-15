using KF.RestApi.Common.Persistence.Entities;
using KF.RestApi.Common.Persistence.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KF.RestApi.Common.Persistence;

public sealed class ApiGatewayDbContext : DbContext
{
    private readonly AuditStoreOptions _auditOptions;

    public DbSet<ApiCallAudit> ApiCallAudits => Set<ApiCallAudit>();
    public DbSet<ApiCallRequestAudit> ApiCallRequestAudits => Set<ApiCallRequestAudit>();
    public DbSet<ApiCallResponseAudit> ApiCallResponseAudits => Set<ApiCallResponseAudit>();

    public ApiGatewayDbContext(
        DbContextOptions<ApiGatewayDbContext> options,
        IOptions<AuditStoreOptions>? auditOptions = null)
        : base(options)
    {
        _auditOptions = auditOptions?.Value ?? AuditStoreOptions.Default;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var schema = string.IsNullOrWhiteSpace(_auditOptions.Schema) ? null : _auditOptions.Schema;
        var tableMode = _auditOptions.TableMode ?? "Single";

        if (string.Equals(tableMode, "Split", StringComparison.OrdinalIgnoreCase))
        {
            ConfigureRequestAudit(modelBuilder, schema);
            ConfigureResponseAudit(modelBuilder, schema);
        }
        else
        {
            ConfigureUnifiedAudit(modelBuilder, schema);
        }
    }

    private void ConfigureUnifiedAudit(ModelBuilder modelBuilder, string? schema)
    {
        var tableName = _auditOptions.TableNameTemplate.Replace("{ApiName}", _auditOptions.DefaultApiName, StringComparison.OrdinalIgnoreCase);
        var audit = modelBuilder.Entity<ApiCallAudit>();

        audit.ToTable(tableName, schema);

        audit.HasKey(x => x.Id);
        audit.Property(x => x.ApiName).IsRequired().HasMaxLength(100);
        audit.Property(x => x.Operation).IsRequired().HasMaxLength(200);
        audit.Property(x => x.Direction).IsRequired().HasMaxLength(20);
        audit.Property(x => x.ErrorCode).HasMaxLength(100);
        audit.Property(x => x.ErrorMessage).HasMaxLength(2000);
        audit.Property(x => x.CorrelationId).HasMaxLength(64);
        audit.Property(x => x.HttpMethod).HasMaxLength(10);
        audit.Property(x => x.RequestPath).HasMaxLength(500);
        audit.Property(x => x.CallerSystem).HasMaxLength(100);

        audit.Property(x => x.RequestTimestampUtc).IsRequired();
        audit.Property(x => x.ResponseTimestampUtc).IsRequired();

        audit.HasIndex(x => x.ApiName);
        audit.HasIndex(x => x.CorrelationId);
        audit.HasIndex(x => new { x.ApiName, x.Operation, x.RequestTimestampUtc });
    }

    private void ConfigureRequestAudit(ModelBuilder modelBuilder, string? schema)
    {
        var tableName = _auditOptions.RequestTableNameTemplate.Replace("{ApiName}", _auditOptions.DefaultApiName, StringComparison.OrdinalIgnoreCase);
        var request = modelBuilder.Entity<ApiCallRequestAudit>();

        request.ToTable(tableName, schema);

        request.HasKey(x => x.Id);
        request.Property(x => x.ApiName).IsRequired().HasMaxLength(100);
        request.Property(x => x.Operation).IsRequired().HasMaxLength(200);
        request.Property(x => x.Direction).IsRequired().HasMaxLength(20);
        request.Property(x => x.ErrorCode).HasMaxLength(100);
        request.Property(x => x.ErrorMessage).HasMaxLength(2000);
        request.Property(x => x.CorrelationId).HasMaxLength(64);
        request.Property(x => x.HttpMethod).HasMaxLength(10);
        request.Property(x => x.RequestPath).HasMaxLength(500);
        request.Property(x => x.CallerSystem).HasMaxLength(100);

        request.Property(x => x.RequestTimestampUtc).IsRequired();
        request.Property(x => x.ResponseTimestampUtc).IsRequired();

        request.HasIndex(x => x.ApiName);
        request.HasIndex(x => x.CorrelationId);
        request.HasIndex(x => new { x.ApiName, x.Operation, x.RequestTimestampUtc });
    }

    private void ConfigureResponseAudit(ModelBuilder modelBuilder, string? schema)
    {
        var tableName = _auditOptions.ResponseTableNameTemplate.Replace("{ApiName}", _auditOptions.DefaultApiName, StringComparison.OrdinalIgnoreCase);
        var response = modelBuilder.Entity<ApiCallResponseAudit>();

        response.ToTable(tableName, schema);

        response.HasKey(x => x.Id);
        response.Property(x => x.ApiName).IsRequired().HasMaxLength(100);
        response.Property(x => x.Operation).IsRequired().HasMaxLength(200);
        response.Property(x => x.Direction).IsRequired().HasMaxLength(20);
        response.Property(x => x.ErrorCode).HasMaxLength(100);
        response.Property(x => x.ErrorMessage).HasMaxLength(2000);
        response.Property(x => x.CorrelationId).HasMaxLength(64);
        response.Property(x => x.HttpMethod).HasMaxLength(10);
        response.Property(x => x.RequestPath).HasMaxLength(500);
        response.Property(x => x.CallerSystem).HasMaxLength(100);

        response.Property(x => x.RequestTimestampUtc).IsRequired();
        response.Property(x => x.ResponseTimestampUtc).IsRequired();

        response.HasIndex(x => x.ApiName);
        response.HasIndex(x => x.CorrelationId);
        response.HasIndex(x => new { x.ApiName, x.Operation, x.RequestTimestampUtc });
    }
}
