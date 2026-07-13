using System.Text.RegularExpressions;

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
        "(SHEA)",
        "(GRAPE)",
        "(SUNFLOWER)"
    ];

    private static readonly Dictionary<string, string> ExactAliases = new(StringComparer.Ordinal)
    {
        ["COCO-CAPRYLATE CAPRATE"] = "COCO-CAPRYLATE/CAPRATE",
        ["AQUA / WATER / EAU"] = "AQUA",
        ["AQUA/WATER/EAU"] = "AQUA",
        ["COPERNICIA CERIFERA CERA / CARNAUBA WAX / CIRE DE CARNAUBA"] = "COPERNICIA CERIFERA CERA",
        ["BUTYROSPERMUM PARKII BUTTER / SHEA BUTTER"] = "BUTYROSPERMUM PARKII BUTTER"
    };

    private static readonly Regex TrademarkSuffix = new(
        @"\s*\([^)]*[®™][^)]*\)\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex FormulaReferenceSuffix = new(
        @"\s*\(F\.?\s*I\.?\s*L\.?\s+[^)]*\)\s*[.;,]?\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex NumericFormulaReferenceSuffix = new(
        @"\s*\.?\(\d+/\d+\)\s*[.;,]?\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

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

        // Remove only an explicit trailing trademark qualifier, for example
        // "Lanolin Alcohol (Eucerit®)". Regulatory qualifiers such as "(NANO)"
        // remain untouched because they do not contain a trademark symbol.
        normalized = TrademarkSuffix.Replace(normalized, string.Empty);

        // Some L'Oréal-family labels append an internal formula reference to
        // the final ingredient, for example "Xanthan Gum (F.I.L. N70032039/1)".
        // It is packaging metadata rather than part of the INCI name. This is
        // intentionally an exact F.I.L. pattern, not a broad bracket remover.
        normalized = FormulaReferenceSuffix.Replace(normalized, string.Empty);
        normalized = NumericFormulaReferenceSuffix.Replace(normalized, string.Empty);

        // Ingredient lists copied from websites often keep the sentence-ending
        // full stop on the final ingredient.
        normalized = normalized.Trim().TrimEnd('.', ';', ',').Trim();

        while (normalized.Contains("  "))
        {
            normalized = normalized.Replace("  ", " ");
        }

        return ExactAliases.GetValueOrDefault(normalized, normalized);
    }
}
