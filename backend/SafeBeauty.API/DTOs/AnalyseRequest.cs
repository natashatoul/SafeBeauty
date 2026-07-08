namespace SafeBeauty.API.DTOs;

public class AnalyseRequest
{
    public List<string> Ingredients { get; set;} = new();

    // Optional profile conditions selected by the user in the frontend.
    // Example values: "Rosacea", "AtopicDermatitis", "Acne".
    // If this list is empty, the backend returns ingredient information
    // without personalised condition flags.
    public List<string> UserConditions { get; set;} = new();
    
}
