using System.Text.RegularExpressions;

namespace SafeBeauty.API.Models;

// Static class = a "toolbox": it holds no state, you never create it with `new`.
// You just call IngredientNormalizer.Normalize("Retinol ") and get back "RETINOL".
public static class IngredientNormalizer
{
    private static readonly string[] NonRegulatoryParentheticalSynonyms =
    [
        "(WATER)",
        "(AQUA)",
        "(FRAGRANCE)",
        "(PARFUM)",
        "(SHEA)",
        "(GRAPE)",
        "(SUNFLOWER)"
    ];
    // it is a dictionary of ingredientd that was detected like unknown but in reality ir is synonims
    private static readonly Dictionary<string, string> ExactAliases = new(StringComparer.Ordinal)
    {
        ["COCO-CAPRYLATE CAPRATE"] = "COCO-CAPRYLATE/CAPRATE",
        ["COCO-CAPRYLATE/CAPRATE(COCO CAPRYLATE/CAPRATE)"] = "COCO-CAPRYLATE/CAPRATE",
        ["WATER"] = "AQUA",
        ["AQUA/WATER/EAU"] = "AQUA",
        ["WATER(AQUA/EAU)"] = "AQUA",
        ["AVENE AQUA"] = "AQUA",
        ["AVENE THERMAL SPRING WATER"] = "AQUA",
        ["BEES WAX"] = "BEESWAX",
        ["SHEA BUTTER"] = "BUTYROSPERMUM PARKII BUTTER",
        ["C10-30 ALKYL ACRYLATE CROSSPOLYMER"] = "ACRYLATES/C10-30 ALKYL ACRYLATE CROSSPOLYMER",
        ["CERA MICROCRISTALLINA"] = "MICROCRYSTALLINE WAX",
        ["COPERNICIA CERIFERA CERA/CARNAUBA WAX/CIRE DE CARNAUBA"] = "COPERNICIA CERIFERA CERA",
        ["BUTYROSPERMUM PARKII BUTTER/SHEA BUTTER"] = "BUTYROSPERMUM PARKII BUTTER",
        ["TRILAURATE-4 PHOSPHATE"] = "TRILAURETH-4 PHOSPHATE",
        ["LACTOBACILLUS/CENTELLA ASIATICA/GLEDITSIA SINENSIS THOM/HOUTTUYNIA CORDATAEXTRACT/ PHELLODENDRON AMURENSE BARK/POLYGONUM CUSPIDATUMROOT/PRUNELLA VULGARIS/TORILIS JAPONICA EXTRACT FERMENT FILTRATE"] =
            "LACTOBACILLUS/CENTELLA ASIATICA/GLEDITSIA SINENSIS THORN/HOUTTUYNIA CORDATA EXTRACT/PHELLODENDRON AMURENSE BARK/POLYGONUM CUSPIDATUM ROOT/PRUNELLA VULGARIS/TORILIS JAPONICA EXTRACT FERMENT FILTRATE"
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

    private static readonly Regex OrganicIngredientAnnotation = new(
        @"\s*\*+\s*ORGANIC\s+INGREDIENT\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex FootnoteMarker = new(
        @"\*+\s*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex NanoSquareBrackets = new(
        @"\[\s*NANO\s*\]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex RepeatedParentheticalName = new(
        @"^(.+)\s+\(\1\)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex BotanicalCommonName = new(
        @"^(?<latin>[A-Z][A-Z.'-]+\s+[A-Z][A-Z.'-]+(?:\s+[A-Z][A-Z.'-]+)?)\s+\((?!NANO\b)[A-Z][A-Z '\-]+\)\s+(?<part>BARK|BRAN|BUD|BULB|BUTTER|CALLUS|EXTRACT|FLOWER|FRUIT|GERM|GUM|HUSK|JUICE|KERNEL|LEAF|OIL|PEEL|POWDER|PULP|RESIN|RHIZOME|ROOT|SEED|SHOOT|SPROUT|STARCH|STEM|WATER|WAX)\b(?<remainder>.*)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);



    private static readonly Regex ColourIndexAlias = new(
        @"^.+?\s+\*?\(\s*(?<ci>CI\s+\d{5})\s*\)\*?$",
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
        normalized = NanoSquareBrackets.Replace(normalized, "(NANO)");
        normalized = OrganicIngredientAnnotation.Replace(normalized, string.Empty);
        normalized = FootnoteMarker.Replace(normalized, string.Empty).Trim();

        // A backslash is not part of INCI nomenclature. Brand labels use it to
        // print the same ingredient in several languages, for example
        // "Water\Aqua\Eau" or "Hordeum Vulgare Extract\Extrait d'Orge".
        // Keep the first label and continue normalising it below.
        var translatedLabelSeparator = normalized.IndexOf('\\');
        if (translatedLabelSeparator >= 0)
        {
            normalized = normalized[..translatedLabelSeparator].Trim();
        }

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

        // Retailer ingredient lists often insert a common English plant name
        // into an otherwise canonical botanical INCI name. Only remove this
        // explanatory bracket when it follows a Latin binomial and is followed
        // by a recognised plant-part/form suffix. Regulatory qualifiers such
        // as "(NANO)" therefore remain intact.
        normalized = BotanicalCommonName.Replace(
            normalized,
            "${latin} ${part}${remainder}");

        // Colour labels may combine a consumer-friendly name with the official
        // Colour Index identifier, e.g. "Yellow 5 (CI 19140)". The CI number is
        // the canonical database name and is more precise than the prefix.
        normalized = ColourIndexAlias.Replace(normalized, "${ci}");

        // Some retailer lists repeat the complete botanical name as a trailing
        // explanation, e.g. "... SEED OIL (... SEED OIL)". This runs after
        // whitespace cleanup so removal of another synonym cannot leave a
        // double space that prevents the exact comparison.
        normalized = RepeatedParentheticalName.Replace(normalized, "$1");
        var aliasKey = normalized.Replace(" (", "(").Replace(" / ", "/");

        return ExactAliases.GetValueOrDefault(aliasKey, normalized);

    }
}
