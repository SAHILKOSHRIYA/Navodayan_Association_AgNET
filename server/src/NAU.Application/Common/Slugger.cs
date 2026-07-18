using System.Text;
using System.Text.RegularExpressions;

namespace NAU.Application.Common;

public static partial class Slugger
{
    /// <summary>Converts a title to a URL-friendly slug (lowercase, hyphenated, ASCII).</summary>
    public static string Slugify(string input)
    {
        var normalized = input.Trim().ToLowerInvariant();
        normalized = NonAlphanumeric().Replace(normalized, "-");
        normalized = MultiHyphen().Replace(normalized, "-").Trim('-');
        return normalized.Length == 0 ? "campaign" : normalized;
    }

    [GeneratedRegex("[^a-z0-9]+")] private static partial Regex NonAlphanumeric();
    [GeneratedRegex("-{2,}")] private static partial Regex MultiHyphen();
}
