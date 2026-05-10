using System.Text;
using System.Text.Json;
using MongoDB.Bson;
using Playbook.Application.Activities;

namespace Playbook.Infrastructure.Mongo;

internal static class CursorHelper
{
    private sealed record CursorPayload(string Sort, string Id, string? DateTicks, string? Title);

    public static string Encode(ActivitySort sort, ObjectId id, DateTime? dateValue, string? title)
    {
        var payload = new CursorPayload(
            sort.ToString(),
            id.ToString(),
            dateValue.HasValue ? dateValue.Value.Ticks.ToString() : null,
            title);
        var json = JsonSerializer.Serialize(payload);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    public static (ActivitySort Sort, ObjectId Id, DateTime? DateValue, string? Title)? Decode(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor)) return null;
        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var payload = JsonSerializer.Deserialize<CursorPayload>(json);
            if (payload is null) return null;
            var sort = Enum.Parse<ActivitySort>(payload.Sort);
            var id = ObjectId.Parse(payload.Id);
            DateTime? dateValue = payload.DateTicks is not null
                ? new DateTime(long.Parse(payload.DateTicks), DateTimeKind.Utc)
                : null;
            return (sort, id, dateValue, payload.Title);
        }
        catch
        {
            return null;
        }
    }
}
