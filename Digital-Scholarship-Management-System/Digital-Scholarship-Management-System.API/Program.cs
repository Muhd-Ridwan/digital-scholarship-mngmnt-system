var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// The CORS: Origins are read from the configuration and not hardcoded
// Thats why make a variable that holds the allowed request API backend from a specific URL
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

// Adding CORS policy to allow Angular dev server to call this API
// So any request coming from localhost:4200 are allowed
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy => policy.WithOrigins(allowedOrigins).WithHeaders("Authorization", "Content-Type").AllowAnyMethod());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// This need to put before authorization to activate the policy in request pipeline.
// So every incoming request got checked against it before proceeding further.
app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

app.Run();
