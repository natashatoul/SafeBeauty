namespace SafeBeauty.API.Services;

public readonly record struct FunctionPurpose(bool SupportsSkin, bool SupportsFormula);

public static class CosmeticFunctionPurposeCatalog
{
    private static readonly HashSet<string> SkinFunctions = new(StringComparer.OrdinalIgnoreCase)
    {
        "ABRASIVE",
        "ANTI-SEBORRHEIC",
        "ANTI-SEBUM",
        "ANTIPERSPIRANT",
        "ASTRINGENT",
        "BLEACHING",
        "CLEANSING",
        "DEODORANT",
        "DEPILATORY",
        "EPILATING",
        "EXFOLIATING",
        "FILM FORMING",
        "KERATOLYTIC",
        "LYTIC",
        "MOISTURISING",
        "OCCLUSIVE",
        "REFATTING",
        "REFRESHING",
        "SKIN CONDITIONING",
        "SKIN CONDITIONING - EMOLLIENT",
        "SKIN CONDITIONING - HUMECTANT",
        "SKIN CONDITIONING - MISCELLANEOUS",
        "SKIN CONDITIONING - OCCLUSIVE",
        "SKIN PROTECTING",
        "SMOOTHING",
        "SOOTHING",
        "SURFACTANT - CLEANSING",
        "TANNING",
        "TONIC",
        "UV FILTER"
    };

    private static readonly HashSet<string> FormulaFunctions = new(StringComparer.OrdinalIgnoreCase)
    {
        "ADHESIVE",
        "ANTICAKING",
        "ANTICORROSIVE",
        "ANTIFOAMING",
        "BINDING",
        "BUFFERING",
        "BULKING",
        "CHELATING",
        "COLORANT",
        "DENATURANT",
        "DISPERSING NON-SURFACTANT",
        "EMULSION STABILISING",
        "FLAVOURING",
        "FOAMING",
        "FRAGRANCE",
        "GEL FORMING",
        "HUMECTANT",
        "LIGHT STABILIZER",
        "OPACIFYING",
        "OXIDISING",
        "PEARLESCENT",
        "PERFUMING",
        "pH ADJUSTERS",
        "PLASTICISER",
        "PRESERVATIVE",
        "PROPELLANT",
        "REDUCING",
        "SLIP MODIFIER",
        "SOLVENT",
        "SURFACE MODIFIER",
        "SURFACTANT",
        "SURFACTANT - DISPERSING",
        "SURFACTANT - EMULSIFYING",
        "SURFACTANT - FOAM BOOSTING",
        "SURFACTANT - HYDROTROPE",
        "SURFACTANT - SOLUBILIZING",
        "UV ABSORBER",
        "VISCOSITY CONTROLLING"
    };

    // These definitions can describe either a direct surface effect or support
    // for the product itself, so the function carries both flags.
    private static readonly HashSet<string> DualPurposeFunctions = new(StringComparer.OrdinalIgnoreCase)
    {
        "ABSORBENT",
        "ANTIMICROBIAL",
        "ANTIOXIDANT"
    };

    public static FunctionPurpose GetPurpose(string functionName)
    {
        var supportsSkin = SkinFunctions.Contains(functionName)
            || DualPurposeFunctions.Contains(functionName);
        var supportsFormula = FormulaFunctions.Contains(functionName)
            || DualPurposeFunctions.Contains(functionName);

        return new FunctionPurpose(supportsSkin, supportsFormula);
    }
}
