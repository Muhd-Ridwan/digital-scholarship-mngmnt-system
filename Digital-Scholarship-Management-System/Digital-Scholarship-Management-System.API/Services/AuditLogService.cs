using System.Globalization;
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

        // Read side for the Admin audit screen. logDate is the partition key, so a date
        // range is one Query per day — never a Scan.
        public async Task<List<AuditLogItem>> QueryAsync(DateTime fromUtc, DateTime toUtc, UserRole? role, string? person)
        {
            var items = new List<AuditLogItem>();

            for (var day = fromUtc.Date; day <= toUtc.Date; day = day.AddDays(1))
            {
                Dictionary<string, AttributeValue>? startKey = null;
                do
                {
                    var response = await _dynamoDb.QueryAsync(new QueryRequest
                    {
                        TableName = _tableName,
                        KeyConditionExpression = "logDate = :d",
                        ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                        {
                            [":d"] = new AttributeValue { S = day.ToString("yyyy-MM-dd") },
                        },
                        ExclusiveStartKey = startKey,
                    });

                    items.AddRange(response.Items.Select(Map));
                    startKey = response.LastEvaluatedKey;
                }
                // A page caps at 1MB; without the loop the tail is silently dropped.
                while (startKey is not null && startKey.Count > 0);
            }

            // Neither role nor person is part of the key, so they filter after the Query.
            IEnumerable<AuditLogItem> filtered = items;
            if (role is not null)
            {
                filtered = filtered.Where(i => i.Role == role);
            }
            if (!string.IsNullOrWhiteSpace(person))
            {
                filtered = filtered.Where(i => i.User.Contains(person, StringComparison.OrdinalIgnoreCase));
            }

            return filtered.OrderByDescending(i => i.Timestamp).ToList();
        }

        private static AuditLogItem Map(Dictionary<string, AttributeValue> item) => new(
            Field(item, "id"),
            Field(item, "user"),
            FromDisplayRole(Field(item, "role")),
            DateTime.TryParse(Field(item, "timestamp"), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var timestamp)
                ? timestamp
                : default,
            Field(item, "action"));

        private static string Field(Dictionary<string, AttributeValue> item, string key) =>
            item.TryGetValue(key, out var value) ? value.S ?? "" : "";

        private static string ToDisplayRole(UserRole role) => role switch
        {
            UserRole.user => "Student",
            UserRole.officer => "Officer",
            UserRole.sponsor => "Sponsor",
            UserRole.admin => "Admin",
            _ => role.ToString(),
        };

        // Inverse of ToDisplayRole. Entries store the display name, but the screen keys its
        // role badges off the enum names — "Student" would render a blank badge.
        private static UserRole? FromDisplayRole(string display) => display switch
        {
            "Student" => UserRole.user,
            "Officer" => UserRole.officer,
            "Sponsor" => UserRole.sponsor,
            "Admin" => UserRole.admin,
            _ => null,
        };
    }

    public record AuditLogItem(string Id, string User, UserRole? Role, DateTime Timestamp, string Action);
}
