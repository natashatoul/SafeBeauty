namespace SafeBeauty.API.Models;

public static class IngredientListParser
{
    private static readonly char[] Separators = [',', '•', ';', '\r', '\n'];

    public static List<string> Parse(IEnumerable<string> entries) => entries
        .SelectMany(entry => (entry ?? string.Empty).Split(
            Separators,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .Where(entry => !string.IsNullOrWhiteSpace(entry))
        .ToList();
}
