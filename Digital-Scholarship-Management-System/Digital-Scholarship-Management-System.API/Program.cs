using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.DynamoDBv2;
using Digital_Scholarship_Management_System.API.Data;
using Digital_Scholarship_Management_System.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHttpClient();

// For JWT From Cognito
var cognitoRegion = builder.Configuration["Cognito:Region"];
var cognitoUserPoolId = builder.Configuration["Cognito:UserPoolId"];
var cognitoAppClientId = builder.Configuration["Cognito:AppClientId"];

// Cognito Admin Client
// Cred are from user secrets.
var awsAccessKey = builder.Configuration["AWS:AccessKey"];
var awsSecretKey = builder.Configuration["AWS:SecretKey"];
var awsCredentials = new BasicAWSCredentials(awsAccessKey, awsSecretKey);
var cognitoRegionEndpoint = RegionEndpoint.GetBySystemName(cognitoRegion);

// S3 Client for document stroage
// Reuse the same AWS credentials
var s3Region = builder.Configuration["S3:Region"];
var s3RegionEndpoint = RegionEndpoint.GetBySystemName(s3Region);

builder.Services.AddSingleton<IAmazonS3>(
    new AmazonS3Client(awsCredentials, s3RegionEndpoint));

// DynamoDB for Audit Log

var dynamoDbRegion = builder.Configuration["DynamoDb:Region"];
var dynamoDbRegionEndpoint = RegionEndpoint.GetBySystemName(dynamoDbRegion);

builder.Services.AddSingleton<IAmazonDynamoDB>(
    new AmazonDynamoDBClient(awsCredentials, dynamoDbRegionEndpoint));

builder.Services.AddSingleton<AuditLogService>();
builder.Services.AddSingleton<AnnouncementService>();

builder.Services.AddSingleton<IAmazonCognitoIdentityProvider>(
    new AmazonCognitoIdentityProviderClient(awsCredentials, cognitoRegionEndpoint)
    );

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://cognito-idp.{cognitoRegion}.amazonaws.com/{cognitoUserPoolId}";
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var clientId = context.Principal?.FindFirst("client_id")?.Value;
                if (clientId != cognitoAppClientId)
                {
                    context.Fail("Token was not issued for this app client.");
                }
                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization();

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

// This need to put before authorization to activate the policy in request pipeline.
// So every incoming request got checked against it before proceeding further.
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
