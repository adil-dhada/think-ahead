using System.Text.Json;

namespace Playbook.Application.Activities;

internal static class RichTextJson
{
    public static bool IsValid(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return true;
        try
        {
            using var _ = JsonDocument.Parse(json);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
