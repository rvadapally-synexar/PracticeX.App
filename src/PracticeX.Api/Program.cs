using PracticeX.Api.SourceDiscovery;
using PracticeX.Infrastructure;
using PracticeX.Infrastructure.Persistence;
using PracticeX.Infrastructure.Tenancy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("CommandCenter", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5173", "https://localhost:5173"];
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("CommandCenter");
app.UseHttpsRedirection();

app.MapGet("/api/system/info", () => Results.Ok(new
{
    product = "PracticeX Command Center",
    posture = "enterprise_data_first",
    database_identifier_policy = "snake_case_unquoted",
    connectors = new[] { "local_folder", "outlook_mailbox" }
}))
.WithName("GetSystemInfo");

app.MapSourceDiscoveryEndpoints();

// Demo seed: creates the default tenant + user the demo current-user resolver
// expects. In production this is replaced by tenant onboarding flows.
if (app.Environment.IsDevelopment() && app.Configuration.GetValue("Seeding:DemoTenant", true))
{
    using var scope = app.Services.CreateScope();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<PracticeXDbContext>();
        await DemoCurrentUserContext.EnsureSeededAsync(db, CancellationToken.None);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Skipped demo seed (database unavailable).");
    }
}

app.Run();
