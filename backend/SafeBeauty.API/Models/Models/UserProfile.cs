using Microsoft.AspNetCore.Identity;

namespace SafeBeauty.API.Models;

public class UserProfile
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? SkinType { get; set; }
    public string? HairCondition { get; set; }
    public string? AgeGroup { get; set; }
    public string? Gender { get; set; }
    public string ConditionsJson { get; set; } = "[]";

    public IdentityUser User { get; set; } = null!;
}
