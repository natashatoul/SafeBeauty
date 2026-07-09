namespace SafeBeauty.API.Models;

// Static class = a "toolbox": it holds no state, you never create it with `new`.
// You just call IngredientNormalizer.Normalize("Retinol ") and get back "RETINOL".
public static class IngredientNormalizer
{
    public static string Normalize(string inciName)
    {
        if (string.IsNullOrWhiteSpace(inciName))
        {
            return string.Empty;
        }

        // Trim() removes leading/trailing spaces (very common in CSV data).
        // ToUpperInvariant() is like ToUpper(), but independent of the OS language.
        // This matters: e.g. in Turkish locale the letter 'i' becomes 'İ' (with a dot),
        // so "Titanium" would normalize differently on a Turkish server than on an English one.
        // Invariant = "always the same, no matter where it runs".
        return inciName.Trim().ToUpperInvariant();
    }
}