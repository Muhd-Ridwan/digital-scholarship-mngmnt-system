using System.Globalization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Digital_Scholarship_Management_System.API.Models;

namespace Digital_Scholarship_Management_System.API.Services
{
    // Announcements in DynamoDB: audience is the partition, sk is createdAt#id so a reverse
    // Query is newest-first. One Query per audience partition — never a Scan.
    public class AnnouncementService
    {
        private readonly IAmazonDynamoDB _dynamoDb;
        private readonly string _tableName;
        private readonly string _readsTableName;

        private static readonly AnnouncementAudience[] AllAudiences =
        [
            AnnouncementAudience.All,
            AnnouncementAudience.Student,
            AnnouncementAudience.Officer,
            AnnouncementAudience.Sponsor,
        ];

        public AnnouncementService(IAmazonDynamoDB dynamoDb, IConfiguration config)
        {
            _dynamoDb = dynamoDb;
            _tableName = config["DynamoDb:AnnouncementsTableName"]!;
            _readsTableName = config["DynamoDb:AnnouncementReadsTableName"]!;
        }

        public async Task<AnnouncementItem> CreateAsync(
            string title,
            string body,
            AnnouncementAudience audience,
            AnnouncementStatus status,
            string createdBy)
        {
            var now = DateTime.UtcNow;
            var id = Guid.NewGuid().ToString();

            var item = new AnnouncementItem(
                id,
                SortKey(now, id),
                title,
                body,
                audience,
                status,
                // Stamp only at publish; drafts keep it null
                status == AnnouncementStatus.Published ? now : null,
                now,
                createdBy);

            await _dynamoDb.PutItemAsync(_tableName, ToAttributes(item));
            return item;
        }

        // Admin view — every status, across all four partitions. Bounded, never a Scan.
        public async Task<List<AnnouncementItem>> ListAllAsync()
        {
            var items = new List<AnnouncementItem>();
            foreach (var audience in AllAudiences)
            {
                items.AddRange(await QueryPartitionAsync(audience, null, int.MaxValue));
            }

            return items.OrderByDescending(i => i.CreatedAt).ToList();
        }

        // Published items for one role: the All partition plus the caller's own. Admin has no
        // audience of its own, so it sees All only.
        public async Task<List<AnnouncementItem>> FeedAsync(AnnouncementAudience? audience, int limit)
        {
            var items = await QueryPartitionAsync(AnnouncementAudience.All, AnnouncementStatus.Published, limit);

            if (audience is not null && audience.Value != AnnouncementAudience.All)
            {
                items.AddRange(await QueryPartitionAsync(audience.Value, AnnouncementStatus.Published, limit));
            }

            return items.OrderByDescending(i => i.CreatedAt).Take(limit).ToList();
        }

        public async Task<AnnouncementItem?> GetAsync(AnnouncementAudience audience, string sk)
        {
            var response = await _dynamoDb.GetItemAsync(_tableName, Key(audience, sk));
            return response.Item is null || response.Item.Count == 0 ? null : Map(response.Item);
        }

        public async Task PutAsync(AnnouncementItem item) =>
            await _dynamoDb.PutItemAsync(_tableName, ToAttributes(item));

        public async Task DeleteAsync(AnnouncementAudience audience, string sk) =>
            await _dynamoDb.DeleteItemAsync(_tableName, Key(audience, sk));

        // One marker per (user, announcement), written only when that user reads it. Re-reading
        // overwrites the same key, so it stays idempotent.
        public async Task MarkReadAsync(string cognitoSub, string announcementId)
        {
            var item = new Dictionary<string, AttributeValue>
            {
                ["cognitoSub"] = new AttributeValue { S = cognitoSub },
                ["announcementId"] = new AttributeValue { S = announcementId },
                ["readAt"] = new AttributeValue { S = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture) },
            };

            await _dynamoDb.PutItemAsync(_readsTableName, item);
        }

        // Everything this user has read. cognitoSub is the partition key, so this is one Query —
        // the reason the table is keyed by user rather than by announcement.
        public async Task<HashSet<string>> ReadIdsAsync(string cognitoSub)
        {
            var ids = new HashSet<string>();
            Dictionary<string, AttributeValue>? startKey = null;

            do
            {
                var response = await _dynamoDb.QueryAsync(new QueryRequest
                {
                    TableName = _readsTableName,
                    KeyConditionExpression = "cognitoSub = :sub",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        [":sub"] = new AttributeValue { S = cognitoSub },
                    },
                    ExclusiveStartKey = startKey,
                });

                foreach (var item in response.Items)
                {
                    ids.Add(Field(item, "announcementId"));
                }

                startKey = response.LastEvaluatedKey;
            }
            while (startKey is not null && startKey.Count > 0);

            return ids;
        }

        // Newest first. Limit caps items *evaluated* and the status filter runs after that, so a
        // partition holding drafts can come back short — keep paging until the take is filled.
        private async Task<List<AnnouncementItem>> QueryPartitionAsync(
            AnnouncementAudience audience,
            AnnouncementStatus? status,
            int take)
        {
            var items = new List<AnnouncementItem>();
            Dictionary<string, AttributeValue>? startKey = null;

            do
            {
                var names = new Dictionary<string, string> { ["#aud"] = "audience" };
                var values = new Dictionary<string, AttributeValue>
                {
                    [":aud"] = new AttributeValue { S = audience.ToString() },
                };

                var request = new QueryRequest
                {
                    TableName = _tableName,
                    KeyConditionExpression = "#aud = :aud",
                    ScanIndexForward = false,
                    ExclusiveStartKey = startKey,
                };

                if (status is not null)
                {
                    // "status" is a DynamoDB reserved word — it only works behind a placeholder
                    names["#st"] = "status";
                    values[":st"] = new AttributeValue { S = status.Value.ToString() };
                    request.FilterExpression = "#st = :st";
                }

                request.ExpressionAttributeNames = names;
                request.ExpressionAttributeValues = values;

                var response = await _dynamoDb.QueryAsync(request);
                items.AddRange(response.Items.Select(Map));
                startKey = response.LastEvaluatedKey;
            }
            while (startKey is not null && startKey.Count > 0 && items.Count < take);

            return take == int.MaxValue ? items : items.Take(take).ToList();
        }

        // Lexicographic order has to equal chronological order, or "newest N" returns N
        // arbitrary items. The id suffix keeps same-instant items distinct.
        private static string SortKey(DateTime createdAt, string id) =>
            $"{createdAt.ToString("O", CultureInfo.InvariantCulture)}#{id}";

        private static Dictionary<string, AttributeValue> Key(AnnouncementAudience audience, string sk) => new()
        {
            ["audience"] = new AttributeValue { S = audience.ToString() },
            ["sk"] = new AttributeValue { S = sk },
        };

        private static Dictionary<string, AttributeValue> ToAttributes(AnnouncementItem item)
        {
            var attributes = new Dictionary<string, AttributeValue>
            {
                ["audience"] = new AttributeValue { S = item.Audience.ToString() },
                ["sk"] = new AttributeValue { S = item.Sk },
                ["announcementId"] = new AttributeValue { S = item.Id },
                ["title"] = new AttributeValue { S = item.Title },
                ["body"] = new AttributeValue { S = item.Body },
                ["status"] = new AttributeValue { S = item.Status.ToString() },
                ["createdAt"] = new AttributeValue { S = item.CreatedAt.ToString("O", CultureInfo.InvariantCulture) },
                ["createdBy"] = new AttributeValue { S = item.CreatedBy },
            };

            if (item.PublishedAt is not null)
            {
                attributes["publishedAt"] =
                    new AttributeValue { S = item.PublishedAt.Value.ToString("O", CultureInfo.InvariantCulture) };
            }

            return attributes;
        }

        private static AnnouncementItem Map(Dictionary<string, AttributeValue> item) => new(
            Field(item, "announcementId"),
            Field(item, "sk"),
            Field(item, "title"),
            Field(item, "body"),
            Enum.TryParse<AnnouncementAudience>(Field(item, "audience"), out var audience)
                ? audience
                : AnnouncementAudience.All,
            Enum.TryParse<AnnouncementStatus>(Field(item, "status"), out var status)
                ? status
                : AnnouncementStatus.Draft,
            ParseDate(Field(item, "publishedAt")),
            ParseDate(Field(item, "createdAt")) ?? default,
            Field(item, "createdBy"));

        private static DateTime? ParseDate(string value) =>
            DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
                ? parsed
                : null;

        private static string Field(Dictionary<string, AttributeValue> item, string key) =>
            item.TryGetValue(key, out var value) ? value.S ?? "" : "";
    }

    public record AnnouncementItem(
        string Id,
        string Sk,
        string Title,
        string Body,
        AnnouncementAudience Audience,
        AnnouncementStatus Status,
        DateTime? PublishedAt,
        DateTime CreatedAt,
        string CreatedBy);
}
