namespace SafeBeauty.API.Models;

// Static class = a "toolbox": it holds no state, you never create it with `new`.
// You just call IngredientNormalizer.Normalize("Retinol ") and get back "RETINOL".
public static class IngredientNormalizer
{
    private static readonly string[] NonRegulatoryParentheticalSynonyms =
    [
        "(WATER)",
        "(FRAGRANCE)",
        "(PARFUM)",
        "(SHEA)"
    ];

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
        var normalized = inciName.Trim().ToUpperInvariant();

        // Product ingredient lists often include explanatory synonyms in brackets:
        //   Aqua (Water), Parfum (Fragrance), Butyrospermum Parkii (Shea) Butter.
        // The official INCI names in CosIng are AQUA, PARFUM and
        // BUTYROSPERMUM PARKII BUTTER, so these human-readable synonyms would
        // otherwise cause false "Unknown ingredient" results.
        //
        // Important: we do NOT remove every bracketed value. Some qualifiers,
        // such as "(NANO)", are regulatory-relevant and exist in official names.
        foreach (var synonym in NonRegulatoryParentheticalSynonyms)
        {
            normalized = normalized.Replace(synonym, string.Empty);
        }

        // Ingredient lists copied from websites often keep the sentence-ending
        // full stop on the final ingredient.
        normalized = normalized.Trim().TrimEnd('.', ';', ',').Trim();

        while (normalized.Contains("  "))
        {
            normalized = normalized.Replace("  ", " ");
        }

        return normalized;
    }
}
