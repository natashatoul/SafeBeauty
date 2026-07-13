using SafeBeauty.API.Models;

namespace SafeBeauty.API.Services;

public static class UvFilterClassifier
{
    public const string AnnexSource = "COSING_Annex_VI_v2";
    public const string CurrentMappingType = "RegulatoryAnnexNormalizedV3";

    private static readonly HashSet<string> MineralFilters = new(StringComparer.Ordinal)
    {
        "TITANIUM DIOXIDE",
        "TITANIUM DIOXIDE (NANO)",
        "ZINC OXIDE",
        "ZINC OXIDE (NANO)"
    };

    private static readonly HashSet<string> OrganicParticulateFilters = new(StringComparer.Ordinal)
    {
        "METHYLENE BIS-BENZOTRIAZOLYL TETRAMETHYLBUTYLPHENOL",
        "METHYLENE BIS-BENZOTRIAZOLYL TETRAMETHYLBUTYLPHENOL (NANO)",
        "TRIS-BIPHENYL TRIAZINE",
        "TRIS-BIPHENYL TRIAZINE (NANO)"
    };

    public static bool IsConfirmedAnnexViMapping(IngredientCategoryMapping mapping) =>
        string.Equals(mapping.Category.Name, "UV Filter", StringComparison.Ordinal) &&
        string.Equals(mapping.Source, AnnexSource, StringComparison.Ordinal) &&
        string.Equals(mapping.MappingType, CurrentMappingType, StringComparison.Ordinal);

    public static string Classify(string inciName)
    {
        var normalizedName = IngredientNormalizer.Normalize(inciName);

        if (MineralFilters.Contains(normalizedName)) return "Mineral / inorganic";
        if (OrganicParticulateFilters.Contains(normalizedName)) return "Organic particulate";
        return "Organic";
    }
}
