using PracticeX.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/api/system/info", () => Results.Ok(new
{
    product = "PracticeX Command Center",
    posture = "enterprise_data_first",
    database_identifier_policy = "snake_case_unquoted",
    connectors = new[] { "local_folder", "outlook_mailbox" }
}))
.WithName("GetSystemInfo");

app.Run();
