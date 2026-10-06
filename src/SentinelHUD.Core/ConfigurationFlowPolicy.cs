using System.Globalization;

namespace SentinelHUD.Core;

/// <summary>
/// Text preparation and action flow for HUD's adapters around the pinned Core components.
/// Core still owns row geometry, colours, painting and controls.
/// </summary>
public static class ConfigurationFlowPolicy
{
    public static string WrapLabel(string text, float width, Func<string, float> measure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentNullException.ThrowIfNull(measure);
        if (!float.IsFinite(width) || width <= 0f)
            throw new ArgumentOutOfRangeException(nameof(width));

        var lines = new List<string>();
        foreach (var paragraph in text.Replace("\r", string.Empty).Split('\n'))
        {
            var remaining = paragraph.Trim();
            while (remaining.Length > 0 && measure(remaining) > width)
            {
                var elements = StringInfo.ParseCombiningCharacters(remaining);
                var end = 0;
                for (var index = 0; index < elements.Length; index++)
                {
                    var candidate = index + 1 < elements.Length ? elements[index + 1] : remaining.Length;
                    if (measure(remaining[..candidate]) > width)
                        break;
                    end = candidate;
                }

                // At least one complete text element must advance, even below a glyph's width.
                if (end == 0)
                    end = elements.Length > 1 ? elements[1] : remaining.Length;
                var space = remaining.LastIndexOf(' ', end - 1, end);
                if (space > 0)
                    end = space;
                lines.Add(remaining[..end].TrimEnd());
                remaining = remaining[end..].TrimStart();
            }
            if (remaining.Length > 0 || paragraph.Trim().Length == 0)
                lines.Add(remaining);
        }
        return string.Join('\n', lines);
    }

    public static bool FitsInline(float previousWidth, float nextWidth, float spacing, float availableWidth)
        => previousWidth + spacing + nextWidth <= availableWidth;
}
