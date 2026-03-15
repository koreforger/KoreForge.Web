using KF.RestApi.Common.Abstractions.Time;
using KF.RestApi.Common.Observability.DependencyInjection;
using KF.RestApi.Common.Persistence.DependencyInjection;
using KF.RestApi.Internal.Sample.DependencyInjection;
using KF.RestApi.Internal.Sample.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services
	.AddCommonObservability()
	.AddCommonPersistence(builder.Configuration)
	.AddSampleInternal(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.MapGet("/health", (IUtcClock clock) =>
{
	return Results.Ok(new
	{
		status = "Healthy",
		timestampUtc = clock.UtcNow
	});
});

app.MapSampleEndpoints();

app.Run();
