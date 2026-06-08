using SafeBeauty.API.Models.Enums;

namespace SafeBeauty.API.Models;

public class ConditionRule
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string EvidenceSource { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    
    public Condition Condition {get; set;} 
    public FlagType FlagType {get; set;} 
    
    public IngredientCategory Category {get; set;} = null!;
    

}