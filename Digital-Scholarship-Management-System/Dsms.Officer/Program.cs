using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.DynamoDBv2;
using Amazon.Lambda.AspNetCoreServer.Hosting;
using Digital_Scholarship_Management_System.API.Data;
using Digital_Scholarship_Management_System.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var cognitoRegion = builder.Configuration["Cognito:Region"];
var cognitoUserPoolId = builder.Configuration["Cognito:UserPoolId"];
var cognitoAppClientId = builder.Configuration["Cognito:AppClientId"];

var awsAccessKey = builder.Configuration["AWS:AccessKey"];
AWSCredentials awsCredentials = string.IsNullOrEmpty(awsAccessKey)
    ? FallbackCredentialsFactory.GetCredentials()
    : new BasicAWSCredentials(awsAccessKey, builder.Configuration["AWS:SecretKey"]);

var s3RegionEndpoint = RegionEndpoint.GetBySystemName(builder.Configuration["S3:Region"]);
var dynamoDbRegionEndpoint = RegionEndpoint.GetBySystemName(builder.Configuration["DynamoDb:Region"]);

builder.Services.AddSingleton<IAmazonS3>(new AmazonS3Client(awsCredentials, s3RegionEndpoint));
builder.Services.AddSingleton<IAmazonDynamoDB>(new AmazonDynamoDBClient(awsCredentials, dynamoDbRegionEndpoint));
builder.Services.AddSingleton<AuditLogService>();

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

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(allowedOrigins).WithHeaders("Authorization", "Content-Type").AllowAnyMethod());
});

builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);

var app = builder.Build();

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();