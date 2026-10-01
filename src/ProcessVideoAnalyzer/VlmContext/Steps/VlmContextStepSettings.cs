using System.Globalization;

namespace ProcessVideoAnalyzer.VlmContext.Steps;

public static class VlmContextStepSettings
{
    public static string String(VlmContextRecipeStep step, string key, string fallback)
    {
        return step.Settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : fallback;
    }

    public static int Int(VlmContextRecipeStep step, string key, int fallback, int min, int max)
    {
        if (!step.Settings.TryGetValue(key, out var value) ||
            !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
        {
            number = fallback;
        }

        return Math.Clamp(number, min, max);
    }

    public static double Double(VlmContextRecipeStep step, string key, double fallback, double min, double max)
    {
        if (!step.Settings.TryGetValue(key, out var value) ||
            !double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            number = fallback;
        }

        return Math.Clamp(number, min, max);
    }

    public static bool Bool(VlmContextRecipeStep step, string key, bool fallback)
    {
        if (!step.Settings.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("on", StringComparison.OrdinalIgnoreCase);
    }
}
