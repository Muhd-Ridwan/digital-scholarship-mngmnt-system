using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Digital_Scholarship_Management_System.API.Models;

namespace Digital_Scholarship_Management_System.API.Services
{
    public class AuditLogService
    {
        private readonly IAmazonDynamoDB _dynamoDb;
        private readonly string _tableName;

        public AuditLogService(IAmazonDynamoDB dynamoDb, IConfiguration config)
        {
            _dynamoDb = dynamoDb;
            _tableName = config["DynamoDb:TableName"]!;
        }

        public async Task LogAsync(User user, string action)
        {
            var now = DateTime.UtcNow;

            var item = new Dictionary<string, AttributeValue>
            {
                ["logDate"] = new AttributeValue { S = now.ToString("yyyy-MM-dd") },
                ["timestamp"] = new AttributeValue { S = now.ToString("O") },
                ["id"] = new AttributeValue { S = Guid.NewGuid().ToString() },
                ["user"] = new AttributeValue { S = user.FullName },
                ["role"] = new AttributeValue { S = ToDisplayRole(user.Role) },
                ["action"] = new AttributeValue { S = action },
            };

            await _dynamoDb.PutItemAsync(_tableName, item);
        }

        private static string ToDisplayRole(UserRole role) => role switch
        {
            UserRole.user => "Student",
            UserRole.officer => "Officer",
            UserRole.sponsor => "Sponsor",
            UserRole.admin => "Admin",
            _ => role.ToString(),
        };
    }
}
